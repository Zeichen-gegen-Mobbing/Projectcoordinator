using System.Net;
using System.Text;
using System.Text.Json;
using FrontEnd.Services;
using Moq;
using Moq.Protected;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Tests.Unit.Services;

public class PlaceServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> httpMessageHandlerMock = new();
    private readonly PlaceService service;
    private readonly HttpClient httpClient;

    public PlaceServiceTests()
    {
        httpClient = new HttpClient(httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };

        service = new PlaceService(httpClient);
    }

    public void Dispose()
    {
        httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetupMockResponse<T>(T responseData, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(responseData);
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }

    private void SetupMockResponseNoContent(HttpStatusCode statusCode = HttpStatusCode.NoContent)
    {
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    public class GetUserPlacesAsync : PlaceServiceTests
    {
        [Test]
        public async Task ReturnsPlaces_WhenApiReturnsData()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            var expectedPlaces = new[]
            {
                new Place
                {
                    Id = new PlaceId("place1"),
                    UserId = userId,
                    Name = "Test Place 1",
                    TransportMode = TransportMode.Car
                },
                new Place
                {
                    Id = new PlaceId("place2"),
                    UserId = userId,
                    Name = "Test Place 2",
                    TransportMode = TransportMode.Car
                }
            };

            SetupMockResponse(expectedPlaces);

            // Act
            var result = await service.GetPlacesAsync(userId);

            // Assert
            var places = result.ToList();
            await Assert.That(places).HasCount().EqualTo(2);
            await Assert.That(places[0].Name).IsEqualTo("Test Place 1");
            await Assert.That(places[1].Name).IsEqualTo("Test Place 2");
        }

        [Test]
        public async Task ReturnsEmptyList_WhenApiReturnsEmptyArray()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            SetupMockResponse(Array.Empty<Place>());

            // Act
            var result = await service.GetPlacesAsync(userId);

            // Assert
            await Assert.That(result).IsEmpty();
        }

        [Test]
        public async Task CallsCorrectEndpoint_WithUserId()
        {
            // Arrange
            var userId = UserId.Parse("12345678-1234-1234-1234-123456789012");
            SetupMockResponse(Array.Empty<Place>());

            // Act
            await service.GetPlacesAsync(userId);

            // Assert
            httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().EndsWith($"/api/users/{userId.Value}/places")),
                ItExpr.IsAny<CancellationToken>());
        }
    }

    public class DeletePlaceAsync : PlaceServiceTests
    {
        [Test]
        public async Task CallsCorrectEndpoint_WithUserIdAndPlaceId()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            var placeId = new PlaceId("place123");
            SetupMockResponseNoContent(HttpStatusCode.NoContent);

            // Act
            await service.DeletePlaceAsync(userId, placeId);

            // Assert
            httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"/api/users/{userId.Value}/places/{placeId.Value}")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task CompletesSuccessfully_WhenApiReturnsNoContent()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            var placeId = new PlaceId("place456");
            SetupMockResponseNoContent(HttpStatusCode.NoContent);

            // Act & Assert
            await service.DeletePlaceAsync(userId, placeId);
            // No exception thrown means success
        }
    }

    public class CreatePlaceForUserAsync : PlaceServiceTests
    {
        [Test]
        public async Task ReturnsPlace_WhenPlaceCreated()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            var name = "New Place";
            var latitude = 52.5200;
            var longitude = 13.4050;

            var expectedPlace = new Place
            {
                Id = new PlaceId("newplace123"),
                UserId = userId,
                Name = name,
                TransportMode = TransportMode.Car
            };

            SetupMockResponse(expectedPlace, HttpStatusCode.Created);

            // Act
            var result = await service.CreatePlaceAsync(userId, name, latitude, longitude, TransportMode.Car);

            // Assert
            await Assert.That(result.Id).IsEqualTo(expectedPlace.Id);
            await Assert.That(result.UserId).IsEqualTo(userId);
            await Assert.That(result.Name).IsEqualTo(name);
        }

        [Test]
        public async Task SendsCorrectPayload_WithoutUserId()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            var name = "Test Place";
            var latitude = 52.5200;
            var longitude = 13.4050;

            var expectedPlace = new Place
            {
                Id = new PlaceId("place123"),
                UserId = userId,
                Name = name,
                TransportMode = TransportMode.Car
            };

            string? capturedContent = null;
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
                {
                    capturedContent = req.Content?.ReadAsStringAsync().Result;
                    var json = JsonSerializer.Serialize(expectedPlace);
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                });

            // Act
            await service.CreatePlaceAsync(userId, name, latitude, longitude, TransportMode.Train);

            // Assert
            await Assert.That(capturedContent).IsNotNull();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = JsonSerializer.Deserialize<JsonElement>(capturedContent!, options);

            // Check using case-insensitive property names
            var hasName = payload.TryGetProperty("name", out var nameElement) || payload.TryGetProperty("Name", out nameElement);
            var hasLatitude = payload.TryGetProperty("latitude", out var latElement) || payload.TryGetProperty("Latitude", out latElement);
            var hasLongitude = payload.TryGetProperty("longitude", out var lonElement) || payload.TryGetProperty("Longitude", out lonElement);
            var hasTransportMode = payload.TryGetProperty("transportMode", out var modeElement) || payload.TryGetProperty("TransportMode", out modeElement);

            await Assert.That(hasName).IsTrue();
            await Assert.That(hasLatitude).IsTrue();
            await Assert.That(hasLongitude).IsTrue();
            await Assert.That(hasTransportMode).IsTrue();
            await Assert.That(nameElement.GetString()).IsEqualTo(name);
            await Assert.That(latElement.GetDouble()).IsEqualTo(latitude);
            await Assert.That(lonElement.GetDouble()).IsEqualTo(longitude);
            await Assert.That(modeElement.GetInt32()).IsEqualTo((int)TransportMode.Train);

            // UserId should NOT be in the payload - it comes from the route
            var hasUserId = payload.TryGetProperty("userId", out _) || payload.TryGetProperty("UserId", out _);
            await Assert.That(hasUserId).IsFalse();
        }

        [Test]
        public async Task CallsCorrectEndpoint_WithUserId()
        {
            // Arrange
            var userId = UserId.Parse("12345678-1234-1234-1234-123456789012");
            var expectedPlace = new Place
            {
                Id = new PlaceId("place123"),
                UserId = userId,
                Name = "Test",
                TransportMode = TransportMode.Car
            };

            SetupMockResponse(expectedPlace, HttpStatusCode.Created);

            // Act
            await service.CreatePlaceAsync(userId, "Test", 0, 0, TransportMode.Car);

            // Assert
            httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().EndsWith($"/api/users/{userId.Value}/places")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task ThrowsException_WhenApiReturnsNull()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                });

            // Act & Assert
            await Assert.That(async () => await service.CreatePlaceAsync(userId, "Test", 0, 0, TransportMode.Car))
                .Throws<InvalidOperationException>()
                .WithMessage("Failed to create place");
        }
    }
}

