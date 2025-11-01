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
        private readonly Mock<ILogger<TrainTransitousService>> loggerMock;
        private readonly TrainTransitousService service;

        public CalculateRoutesAsync()
        {
            httpHandlerMock = new Mock<HttpMessageHandler>();
            clientFactoryMock = new Mock<IHttpClientFactory>();
            loggerMock = new Mock<ILogger<TrainTransitousService>>();

            var httpClient = new HttpClient(httpHandlerMock.Object)
            {
                BaseAddress = new Uri("https://api.transitous.org/")
            };
            clientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            service = new TrainTransitousService(clientFactoryMock.Object, loggerMock.Object);
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
        /// Given: A valid list of places, origin coordinates, and car costs task
        /// When: Transitous API returns successful response with durations
        /// Then: Returns TrainRouteResult for each place with train time and car costs
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

            var carCosts = CreateCarCosts(("place1", 150), ("place2", 300));

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new[] { new[] { 800.0 }, new[] { 1200.0 } },
                distances = new[] { new[] { 4000.0 }, new[] { 9000.0 } }
            });


            // Act
            var results = (await service.CalculateRoutesAsync(
                places, 52.5100, 13.4000, Task.FromResult(carCosts))).ToList();

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
        /// Given: Car costs task is not yet completed when starting train calculation
        /// When: Train API call completes
        /// Then: Awaits car costs and combines results correctly (parallel execution)
        /// </summary>
        [Test]
        public async Task AwaitsCarCosts_WhenCarCostsNotYetAvailable()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new[] { new[] { 800.0 } },
                distances = new[] { new[] { 4000.0 } }
            });

            

            // Simulate car costs arriving after delay
            var carCostsTask = Task.Run(async () =>
            {
                await Task.Delay(100);
                return CreateCarCosts(("place1", 150));
            });

            // Act
            var results = (await service.CalculateRoutesAsync(
                places, 52.5100, 13.4000, carCostsTask)).ToList();

            // Assert
            await Assert.That(results.Single().CostCents).IsEqualTo((ushort)150);
        }

        /// <summary>
        /// Given: Empty list of places
        /// When: Calling CalculateRoutesAsync
        /// Then: Returns empty result set without calling API
        /// </summary>
        [Test]
        public async Task ReturnsEmpty_WhenNoPlacesProvided()
        {
            // Arrange
            var places = Enumerable.Empty<PlaceEntity>();
            var carCosts = Task.FromResult(new Dictionary<PlaceId, ushort>());
            SetupHttpResponse(HttpStatusCode.OK, "{}");


            // Act
            var results = await service.CalculateRoutesAsync(places, 52.5100, 13.4000, carCosts);

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
            var carCosts = Task.FromResult(CreateCarCosts(("place1", 150)));
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");


            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000, carCosts));
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
            var carCosts = Task.FromResult(CreateCarCosts(("place1", 150)));
            SetupHttpResponse(HttpStatusCode.OK, "{ invalid json }");


            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000, carCosts));
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
            var carCosts = Task.FromResult(CreateCarCosts(("place1", 150)));
            
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
            await service.CalculateRoutesAsync(places, 52.5100, 13.4000, carCosts);

            // Assert
            await Assert.That(capturedRequest).IsNotNull();
            await Assert.That(capturedRequest!.Method).IsEqualTo(HttpMethod.Post);
            await Assert.That(capturedRequest.RequestUri).IsNotNull();
        }

        /// <summary>
        /// Given: Car costs task throws exception
        /// When: Awaiting car costs in train calculation
        /// Then: Propagates the exception from car costs task
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenCarCostsTaskFails()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
            var carCostsTask = Task.FromException<Dictionary<PlaceId, ushort>>(
                new InvalidOperationException("Car cost calculation failed"));

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new[] { new[] { 800.0 } },
                distances = new[] { new[] { 4000.0 } }
            });


            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000, carCostsTask));

            await Assert.That(exception!.Message).IsEqualTo("Car cost calculation failed");
        }

    }
}
