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
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
            var json = JsonSerializer.Serialize(response, _jsonOptions);
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
        /// Then: Returns average duration of all itineraries
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

            await Assert.That(result.DurationSeconds).IsEqualTo((uint)800);
            await Assert.That(result.CostCents).IsEqualTo((uint)150);
        }

        /// <summary>
        /// Given: Transitous API returns more than 5 itineraries
        /// When: Calling CalculateRoutesAsync
        /// Then: Returns average duration of all itineraries excluding the longest 20%
        /// </summary>
        [Test]
        public async Task ExcludesLongest20Percent_WhenMultipleItinerariesReturned()
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

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                itineraries = new[]
                {
                    new { duration = 2 },
                    new { duration = 3 },
                    new { duration = 5 },
                    new { duration = 7 },
                    new { duration = 11 },
                    new { duration = 13 },
                },
                direct = Array.Empty<object>()
            });

            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Count).IsEqualTo(1);
            var result = results.Single();

            await Assert.That(result.DurationSeconds).IsEqualTo((uint)5);
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

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                itineraries = new[]
                {
                    new { duration = 2 },
                    new { duration = 3 }
                },
                direct = new[]
                {
                    new { duration = 5 },
                    new { duration = 7 }
                }
            });

            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Count).IsEqualTo(1);
            var result = results.Single();

            await Assert.That(result.DurationSeconds).IsEqualTo((uint)4);
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
                    }, _jsonOptions))
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
