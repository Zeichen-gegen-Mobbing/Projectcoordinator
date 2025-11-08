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

public class CarOpenRouteServiceTests
{
    public class CalculateRoutesAsync
    {
        private readonly Mock<HttpMessageHandler> httpHandlerMock;
        private readonly Mock<IHttpClientFactory> clientFactoryMock;
        private readonly Mock<IOptions<OpenRouteServiceOptions>> optionsMock;
        private readonly Mock<ILogger<CarOpenRouteService>> loggerMock;
        private readonly CarOpenRouteService service;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public CalculateRoutesAsync()
        {
            httpHandlerMock = new Mock<HttpMessageHandler>();
            clientFactoryMock = new Mock<IHttpClientFactory>();
            optionsMock = new Mock<IOptions<OpenRouteServiceOptions>>();
            loggerMock = new Mock<ILogger<CarOpenRouteService>>();

            optionsMock.Setup(o => o.Value).Returns(new OpenRouteServiceOptions
            {
                Title = "OpenRouteService",
                ApiKey = "test-api-key",
                BaseUrl = "https://api.openrouteservice.org/"
            });

            var httpClient = new HttpClient(httpHandlerMock.Object)
            {
                BaseAddress = new Uri("https://api.openrouteservice.org/")
            };
            clientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            service = new CarOpenRouteService(clientFactoryMock.Object, optionsMock.Object, loggerMock.Object);
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
        /// When: OpenRouteService returns successful response with durations and distances
        /// Then: Returns CarRouteResult for each place with correct time, distance, and calculated cost
        /// </summary>
        [Test]
        public async Task ReturnsCarRouteResults_WhenApiReturnsSuccess()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home"),
                CreatePlace("place2", "Office")
            };

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new double[][] { [600.0], [900.0] },
                distances = new double[][] { [5000.0], [10000.0] }
            });


            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Count).IsEqualTo(2);

            var result1 = results.First(r => r.PlaceId == places[0].Id);
            await Assert.That(result1.DurationSeconds).IsEqualTo((uint)600);
            await Assert.That(result1.DistanceMeters).IsEqualTo((uint)5000);
            await Assert.That(result1.CostCents).IsEqualTo((uint)125); // ceil(5000/1000) * 25 = 5 * 25 = 125

            var result2 = results.First(r => r.PlaceId == places[1].Id);
            await Assert.That(result2.DurationSeconds).IsEqualTo((uint)900);
            await Assert.That(result2.DistanceMeters).IsEqualTo((uint)10000);
            await Assert.That(result2.CostCents).IsEqualTo((uint)250); // ceil(10000/1000) * 25 = 10 * 25 = 250
        }

        /// <summary>
        /// Given: A place with fractional kilometer distance (e.g., 5500m = 5.5km)
        /// When: Calculating cost
        /// Then: Cost is rounded up (ceil) to next kilometer: ceil(5.5) * 25 = 150 cents
        /// </summary>
        [Test]
        public async Task RoundsUpCostToNextKilometer_WhenDistanceHasFraction()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };

            SetupHttpResponse(HttpStatusCode.OK, new
            {
                durations = new double[][] { [600.0] },
                distances = new double[][] { [5500.0] } // 5.5 km
            });


            // Act
            var results = (await service.CalculateRoutesAsync(places, 52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(results.Single().CostCents).IsEqualTo((uint)150); // ceil(5.5) * 25 = 6 * 25 = 150
        }

        /// <summary>
        /// Given: Empty list of places
        /// When: Calling CalculateRoutesAsync
        /// Then: Returns empty result set (no API call needed)
        /// </summary>
        [Test]
        public async Task ReturnsEmpty_WhenNoPlacesProvided()
        {
            // Arrange
            List<PlaceEntity> places = [];
            SetupHttpResponse(HttpStatusCode.OK, "{}");


            // Act
            var results = await service.CalculateRoutesAsync(places, 52.5100, 13.4000);

            // Assert
            await Assert.That(results.Count()).IsEqualTo(0);
        }

        /// <summary>
        /// Given: OpenRouteService API returns error status code
        /// When: Calling CalculateRoutesAsync
        /// Then: Throws exception with meaningful error message
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
            SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");


            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000));
        }

        /// <summary>
        /// Given: OpenRouteService returns malformed JSON
        /// When: Calling CalculateRoutesAsync
        /// Then: Throws exception with deserialization error
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenApiReturnsInvalidJson()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
            SetupHttpResponse(HttpStatusCode.OK, "{ invalid json }");


            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await service.CalculateRoutesAsync(places, 52.5100, 13.4000));
        }

        /// <summary>
        /// Given: OpenRouteService returns successful response
        /// When: Calling CalculateRoutesAsync
        /// Then: Sends correct request to OpenRouteService with driving-car profile
        /// </summary>
        [Test]
        public async Task SendsCorrectRequest_WhenCalculatingRoutes()
        {
            // Arrange
            var places = new List<PlaceEntity> { CreatePlace("place1") };
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
                        durations = new double[][] { [600.0] },
                        distances = new double[][] { [5000.0] }
                    }, _jsonOptions))
                });


            // Act
            await service.CalculateRoutesAsync(places, 52.5100, 13.4000);

            // Assert
            await Assert.That(capturedRequest).IsNotNull();
            await Assert.That(capturedRequest!.Method).IsEqualTo(HttpMethod.Post);
            await Assert.That(capturedRequest.RequestUri!.ToString().Contains("v2/matrix/driving-car")).IsTrue();
        }
    }
}
