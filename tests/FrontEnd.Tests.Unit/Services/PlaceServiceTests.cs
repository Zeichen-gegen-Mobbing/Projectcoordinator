using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FrontEnd.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Tests.Unit.Services;

public class PlaceServiceTests : IDisposable
{
    protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    protected readonly Mock<ILogger<PlaceService>> _loggerMock = new();
    protected readonly Mock<AuthenticationStateProvider> _authStateProviderMock = new();
    protected readonly HttpClient _httpClient;
    protected readonly PlaceService _service;
    protected readonly Guid _testUserId = Guid.NewGuid();

    public PlaceServiceTests()
    {
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.test.com/api/")
        };

        var claims = new[]
        {
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", _testUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(user);

        _authStateProviderMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(authState);

        _service = new PlaceService(_httpClient, _loggerMock.Object, _authStateProviderMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    public class GetAllPlacesAsync : PlaceServiceTests
    {
        [Test]
        public async Task ReturnsPlaces_WhenApiReturnsSuccess()
        {
            // Arrange
            var places = new[]
            {
                new Place { Id = new PlaceId("1"), UserId = new UserId(_testUserId), Name = "Place 1" },
                new Place { Id = new PlaceId("2"), UserId = new UserId(_testUserId), Name = "Place 2" }
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(places)
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains($"users/{_testUserId}/places")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetAllPlacesAsync();

            // Assert
            var resultList = result.ToList();
            await Assert.That(resultList).HasCount().EqualTo(2);
            await Assert.That(resultList[0].Name).IsEqualTo("Place 1");
        }

        [Test]
        public async Task ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () => await _service.GetAllPlacesAsync());
        }
    }

    public class CreatePlaceAsync : PlaceServiceTests
    {
        [Test]
        public async Task CreatesPlace_WhenApiReturnsSuccess()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.Created);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains($"users/{_testUserId}/places")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.CreatePlaceAsync("Test Place", 10.5, 20.5);

            // Assert
            await Assert.That(result.Name).IsEqualTo("Test Place");
            await Assert.That(result.UserId.Value).IsEqualTo(_testUserId);
        }

        [Test]
        public async Task ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () => 
                await _service.CreatePlaceAsync("Test", 10.5, 20.5));
        }
    }

    public class UpdatePlaceAsync : PlaceServiceTests
    {
        [Test]
        public async Task UpdatesPlace_WhenApiReturnsSuccess()
        {
            // Arrange
            var placeId = new PlaceId("place-123");
            var updatedPlace = new Place 
            { 
                Id = placeId, 
                UserId = new UserId(_testUserId), 
                Name = "Updated Place" 
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(updatedPlace)
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Put &&
                        req.RequestUri!.ToString().Contains($"users/{_testUserId}/places/{placeId.Value}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.UpdatePlaceAsync(placeId, "Updated Place", 15.5, 25.5);

            // Assert
            await Assert.That(result.Name).IsEqualTo("Updated Place");
            await Assert.That(result.Id).IsEqualTo(placeId);
        }

        [Test]
        public async Task ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            var placeId = new PlaceId("place-123");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () => 
                await _service.UpdatePlaceAsync(placeId, "Updated", 15.5, 25.5));
        }
    }

    public class DeletePlaceAsync : PlaceServiceTests
    {
        [Test]
        public async Task DeletesPlace_WhenApiReturnsSuccess()
        {
            // Arrange
            var placeId = new PlaceId("place-123");
            var response = new HttpResponseMessage(HttpStatusCode.NoContent);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete &&
                        req.RequestUri!.ToString().Contains($"users/{_testUserId}/places/{placeId.Value}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            await _service.DeletePlaceAsync(placeId);

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"users/{_testUserId}/places/{placeId.Value}")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            var placeId = new PlaceId("place-123");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () => 
                await _service.DeletePlaceAsync(placeId));
        }
    }
}
