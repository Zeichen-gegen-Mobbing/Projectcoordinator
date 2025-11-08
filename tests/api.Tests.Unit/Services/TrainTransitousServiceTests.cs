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

            var options = Microsoft.Extensions.Options.Options.Create(new TransitousOptions
            {
                Title = "Transitous",
                BaseUrl = "https://api.transitous.org"
            });

            service = new TrainTransitousService(clientFactoryMock.Object, carServiceMock.Object, options, loggerMock.Object);
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
                itineraries = new[]
                {
                    new { duration = 800 }
                },
                direct = Array.Empty<object>()
            });


            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Count).IsEqualTo(2);

            var result1 = results.Single(r => r.PlaceId == places[0].Id);
            await Assert.That(result1.DurationSeconds).IsEqualTo((uint)800); // Train time (average of outbound and return)
            await Assert.That(result1.CostCents).IsEqualTo((uint)150); // Car cost

            var result2 = results.Single(r => r.PlaceId == places[1].Id);
            await Assert.That(result2.DurationSeconds).IsEqualTo((uint)800); // Train time (average of outbound and return)
            await Assert.That(result2.CostCents).IsEqualTo((uint)300); // Car cost
        }

        /// <summary>
        /// Given: Transitous API returns multiple itineraries
        /// When: Calling CalculateRoutesAsync
        /// Then: Returns average duration of all itineraries for each direction, then averages both directions
        /// </summary>
        [Test]
        public async Task UsesAverageOfAllItineraries_WhenMultipleItinerariesReturned()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home")
            };

            var carResults = new List<CarRouteResult>
            {
                new() { PlaceId = PlaceId.Parse("place1"), CostCents = 150, DurationSeconds = 600, DistanceMeters = 5000 }
            };

            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ReturnsAsync(carResults);

            // Return multiple itineraries with different durations
            // First call (outbound): 600, 800, 1000 -> average = 800
            // Second call (return): 700, 900, 1100 -> average = 900
            // Overall average: (800 + 900) / 2 = 850
            SetupHttpResponse(HttpStatusCode.OK, new
            {
                itineraries = new[]
                {
                    new { duration = 600 },
                    new { duration = 800 },
                    new { duration = 1000 }
                },
                direct = Array.Empty<object>()
            });

            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Count).IsEqualTo(1);
            var result = results.Single();

            // Average of outbound (600, 800, 1000 = avg 800) and return (same response = avg 800) = 800
            await Assert.That(result.DurationSeconds).IsEqualTo((uint)800);
            await Assert.That(result.CostCents).IsEqualTo((uint)150);
        }

        /// <summary>
        /// Given: Transitous API returns both itineraries and direct routes
        /// When: Calling CalculateRoutesAsync
        /// Then: Returns the lower average of itineraries or direct routes
        /// </summary>
        /// <remarks>Unclear if the API would actually do that, but we want to handle both cases anyway</remarks>
        [Test]
        public async Task ReturnsLowerAverage_WhenBothItinerariesAndDirectProvided()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Place 1")
            };

            var carResults = new List<CarRouteResult>
            {
                new() { PlaceId = PlaceId.Parse("place1"), CostCents = 150, DurationSeconds = 600, DistanceMeters = 5000 }
            };

            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(places, 52.5100, 13.4000))
                .ReturnsAsync(carResults);

            // Return both itineraries and direct routes
            // Itineraries: 1000, 1200 -> average = 1100
            // Direct: 500, 700 -> average = 600
            // Should return 600 (lower average)
            SetupHttpResponse(HttpStatusCode.OK, new
            {
                itineraries = new[]
                {
                    new { duration = 1000 },
                    new { duration = 1200 }
                },
                direct = new[]
                {
                    new { duration = 500 },
                    new { duration = 700 }
                }
            });

            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Count).IsEqualTo(1);
            var result = results.Single();

            // Should return average of direct (600) since it's lower than itineraries (1100)
            await Assert.That(result.DurationSeconds).IsEqualTo((uint)600);
            await Assert.That(result.CostCents).IsEqualTo((uint)150);
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
            List<PlaceEntity> places = [];

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
            await Assert.ThrowsAsync<Exceptions.ProblemDetailsException>(async () =>
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
            await Assert.ThrowsAsync<Exceptions.ProblemDetailsException>(async () =>
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
                        itineraries = new[]
                        {
                            new { duration = 800 }
                        },
                        direct = Array.Empty<object>()
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
                });


            // Act
            await service.CalculateRoutesAsync(places, 52.5100, 13.4000);

            // Assert
            await Assert.That(capturedRequest).IsNotNull();
            await Assert.That(capturedRequest!.Method).IsEqualTo(HttpMethod.Get);
            await Assert.That(capturedRequest.RequestUri).IsNotNull();
            await Assert.That(capturedRequest.Headers.UserAgent.ToString()).Contains("Projectcoordinator");
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
                itineraries = new[]
                {
                    new { duration = 800 }
                },
                direct = Array.Empty<object>()
            });


            // Act & Assert - Task.WhenAll wraps exceptions in AggregateException
            var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000));

            await Assert.That(exception!.InnerException).IsNotNull();
            await Assert.That(exception.InnerException).IsTypeOf<InvalidOperationException>();
            await Assert.That(exception.InnerException!.Message).IsEqualTo("Car cost calculation failed");
        }

    }
}
