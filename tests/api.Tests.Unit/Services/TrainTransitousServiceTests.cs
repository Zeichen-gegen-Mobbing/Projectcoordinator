using api.Entities;
using api.Models;
using api.Options;
using api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Services;

public class TrainTransitousServiceTests
{
    public class CalculateRoutesAsync
    {
        private readonly Mock<HttpMessageHandler> httpHandlerMock;
        private readonly Mock<IHttpClientFactory> clientFactoryMock;
        private readonly Mock<ICarRouteService> carServiceMock;
        private readonly Mock<ILogger<TrainTransitousService>> loggerMock;
        private readonly TrainTransitousService service;

        public CalculateRoutesAsync()
        {
            httpHandlerMock = new Mock<HttpMessageHandler>();
            clientFactoryMock = new Mock<IHttpClientFactory>();
            carServiceMock = new Mock<ICarRouteService>();
            loggerMock = new Mock<ILogger<TrainTransitousService>>();

            var httpClient = new HttpClient(httpHandlerMock.Object)
            {
                BaseAddress = new Uri("https://api.transitous.org/")
            };
            clientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            service = new TrainTransitousService(clientFactoryMock.Object, carServiceMock.Object, loggerMock.Object);
        }

        private void SetupHttpResponse<T>(HttpStatusCode statusCode, T response)
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            SetupHttpResponse(statusCode, json);
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            httpHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }

        private static PlaceEntity CreatePlace(string id, string name = "Test Place") => new()
        {
            Id = PlaceId.Parse(id),
            UserId = UserId.Parse(Guid.NewGuid().ToString()),
            Name = name,
            Latitude = 52.5200,
            Longitude = 13.4050
        };

        private static Dictionary<PlaceId, ushort> CreateCarCosts(params (string id, ushort cost)[] costs)
        {
            return costs.ToDictionary(c => PlaceId.Parse(c.id), c => c.cost);
        }

        /// <summary>
        /// Given: A valid list of places and origin coordinates
        /// When: Transitous API returns successful response with durations
        /// Then: Returns TrainRouteResult for each place with train time and car costs from CarService
        /// </summary>
        [Test]
        public async Task ReturnsTrainRouteResults_WhenApiReturnsSuccess()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home"),
                CreatePlace("place2", "Office")
            };

            var carResults = new List<CarRouteResult>
            {
                new() { PlaceId = PlaceId.Parse("place1"), CostCents = 150, DurationSeconds = 600, DistanceMeters = 5000 },
                new() { PlaceId = PlaceId.Parse("place2"), CostCents = 300, DurationSeconds = 900, DistanceMeters = 10000 }
            };

            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ReturnsAsync(carResults);

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new[] { new[] { 800.0 }, new[] { 1200.0 } },
                distances = new[] { new[] { 4000.0 }, new[] { 9000.0 } }
            });


            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Count).IsEqualTo(2);

            var result1 = results.First(r => r.PlaceId == places[0].Id);
            await Assert.That(result1.DurationSeconds).IsEqualTo(800.0); // Train time
            await Assert.That(result1.CostCents).IsEqualTo((ushort)150); // Car cost

            var result2 = results.First(r => r.PlaceId == places[1].Id);
            await Assert.That(result2.DurationSeconds).IsEqualTo(1200.0); // Train time
            await Assert.That(result2.CostCents).IsEqualTo((ushort)300); // Car cost
        }

        /// <summary>
        /// Given: CarService takes time to calculate routes
        /// When: Train API call completes
        /// Then: Awaits car routes and combines results correctly (parallel execution)
        /// </summary>
        [Test]
        public async Task AwaitsCarRoutes_WhenCarRoutesNotYetAvailable()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new[] { new[] { 800.0 } },
                distances = new[] { new[] { 4000.0 } }
            });

            // Simulate car service taking time
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .Returns(async () =>
                {
                    await Task.Delay(100);
                    return new List<CarRouteResult>
                    {
                        new() { PlaceId = PlaceId.Parse("place1"), CostCents = 150, DurationSeconds = 600, DistanceMeters = 5000 }
                    };
                });

            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Single().CostCents).IsEqualTo((ushort)150);
        }

        /// <summary>
        /// Given: Empty list of places
        /// When: Calling CalculateRoutesAsync
        /// Then: Returns empty result set without calling APIs
        /// </summary>
        [Test]
        public async Task ReturnsEmpty_WhenNoPlacesProvided()
        {
            // Arrange
            var places = Enumerable.Empty<PlaceEntity>();
            
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ReturnsAsync(Enumerable.Empty<CarRouteResult>());
            
            SetupHttpResponse(HttpStatusCode.OK, "{}");


            // Act
            var results = await service.CalculateRoutesAsync(places, 52.5100, 13.4000);

            // Assert
            await Assert.That(results.Count()).IsEqualTo(0);
        }

        /// <summary>
        /// Given: Transitous API returns error status code
        /// When: Calling CalculateRoutesAsync
        /// Then: Throws exception with meaningful error message
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
            
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ReturnsAsync(new List<CarRouteResult>
                {
                    new() { PlaceId = PlaceId.Parse("place1"), CostCents = 150, DurationSeconds = 600, DistanceMeters = 5000 }
                });
            
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");


            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000));
        }

        /// <summary>
        /// Given: Transitous API returns malformed JSON
        /// When: Calling CalculateRoutesAsync
        /// Then: Throws exception with deserialization error
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenApiReturnsInvalidJson()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
            
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ReturnsAsync(new List<CarRouteResult>
                {
                    new() { PlaceId = PlaceId.Parse("place1"), CostCents = 150, DurationSeconds = 600, DistanceMeters = 5000 }
                });
            
            SetupHttpResponse(HttpStatusCode.OK, "{ invalid json }");


            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000));
        }

        /// <summary>
        /// Given: Transitous API returns successful response
        /// When: Calling CalculateRoutesAsync
        /// Then: Sends correct request to Transitous API
        /// </summary>
        [Test]
        public async Task SendsCorrectRequest_WhenCalculatingRoutes()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
            
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ReturnsAsync(new List<CarRouteResult>
                {
                    new() { PlaceId = PlaceId.Parse("place1"), CostCents = 150, DurationSeconds = 600, DistanceMeters = 5000 }
                });
            
            HttpRequestMessage? capturedRequest = null;
            httpHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        durations = new[] { new[] { 800.0 } },
                        distances = new[] { new[] { 4000.0 } }
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
                });


            // Act
            await service.CalculateRoutesAsync(places, 52.5100, 13.4000);

            // Assert
            await Assert.That(capturedRequest).IsNotNull();
            await Assert.That(capturedRequest!.Method).IsEqualTo(HttpMethod.Post);
            await Assert.That(capturedRequest.RequestUri).IsNotNull();
        }

        /// <summary>
        /// Given: CarService throws exception
        /// When: Awaiting car routes in train calculation
        /// Then: Propagates the exception from CarService
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenCarServiceFails()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
            
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ThrowsAsync(new InvalidOperationException("Car cost calculation failed"));

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new[] { new[] { 800.0 } },
                distances = new[] { new[] { 4000.0 } }
            });


            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000));

            await Assert.That(exception!.Message).IsEqualTo("Car cost calculation failed");
        }

    }
}
