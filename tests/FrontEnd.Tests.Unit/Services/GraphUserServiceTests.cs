using System.Net;
using System.Net.Http.Json;
using FrontEnd.Models;
using FrontEnd.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Tests.Unit.Services;

public class GraphUserServiceTests : IDisposable
{
    protected readonly Mock<ILogger<GraphUserService>> _loggerMock = new();
    protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    protected readonly GraphUserService _service;
    protected readonly HttpClient _httpClient;

    public GraphUserServiceTests()
    {
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        _service = new GraphUserService(_httpClient, _loggerMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    public class GetUserAsyncMethod : GraphUserServiceTests
    {
        [Test]
        public async Task ReturnsUserWithDisplayName_WhenGraphApiReturnsSuccess()
        {
            // Arrange
            var userId = new UserId(Guid.NewGuid());
            var expectedDisplayName = "John Doe";
            var graphUser = new GraphUser { Id = userId.Value.ToString(), DisplayName = expectedDisplayName };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(graphUser)
                });

            // Act
            var result = await _service.GetUserAsync(userId);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Id).IsEqualTo(userId);
            await Assert.That(result.DisplayName).IsEqualTo(expectedDisplayName);
        }

        [Test]
        public async Task CallsCorrectEndpoint_WhenGetUserAsyncIsCalled()
        {
            // Arrange
            var userId = new UserId(Guid.NewGuid());
            var graphUser = new GraphUser { Id = userId.Value.ToString(), DisplayName = "Test User" };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(graphUser)
                });

            // Act
            var user = await _service.GetUserAsync(userId);

            // Assert
            await Assert.That(user.DisplayName).IsEqualTo(graphUser.DisplayName);
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Equals($"https://graph.microsoft.com/v1.0/users/{userId.Value}?$select=displayName,id")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task ThrowsInvalidOperationException_WhenGraphApiReturnsNull()
        {
            // Arrange
            var userId = new UserId(Guid.NewGuid());

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create<GraphUser?>(null)
                });

            // Act
            var act = async () =>
            {
                await _service.GetUserAsync(userId);
            };

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Test]
        public async Task ThrowsHttpRequestException_WhenGraphApiReturnsErrorStatus()
        {
            // Arrange
            var userId = new UserId(Guid.NewGuid());

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"User not found\"}}")
                });

            // Act
            var act = async () =>
            {
                await _service.GetUserAsync(userId);
            };

            // Assert
            await Assert.ThrowsAsync<HttpRequestException>(act);
        }

        [Test]
        public async Task PreservesUserId_WhenDisplayNameIsRetrieved()
        {
            // Arrange
            var userId = new UserId(Guid.NewGuid());
            var displayName = "Display Name";
            var graphUser = new GraphUser { Id = userId.Value.ToString(), DisplayName = displayName };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(graphUser)
                });

            // Act
            var result = await _service.GetUserAsync(userId);

            // Assert
            await Assert.That(result.Id.Value).IsEqualTo(userId.Value);
            await Assert.That(result.DisplayName).IsEqualTo(displayName);
        }

        /// <summary>
        /// Given: Multiple calls to GetUserAsync with different user IDs
        /// When: All calls succeed
        /// Then: Each call returns the correct user data
        /// </summary>
        [Test]
        public async Task ReturnsCorrectUser_WhenCalledMultipleTimes()
        {
            // Arrange
            var userId1 = new UserId(Guid.NewGuid());
            var userId2 = new UserId(Guid.NewGuid());
            var graphUser1 = new GraphUser { Id = userId1.Value.ToString(), DisplayName = "Alice Smith" };
            var graphUser2 = new GraphUser { Id = userId2.Value.ToString(), DisplayName = "Bob Johnson" };

            _httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(graphUser1)
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(graphUser2)
                });

            // Act
            var result1 = await _service.GetUserAsync(userId1);
            var result2 = await _service.GetUserAsync(userId2);

            // Assert
            await Assert.That(result1.Id.Value).IsEqualTo(userId1.Value);
            await Assert.That(result1.DisplayName).IsEqualTo("Alice Smith");
            await Assert.That(result2.Id.Value).IsEqualTo(userId2.Value);
            await Assert.That(result2.DisplayName).IsEqualTo("Bob Johnson");
        }

        [Test]
        public async Task HandlesUserWithoutDisplayName_WhenGraphReturnsNullDisplayName()
        {
            // Arrange
            var userId = new UserId(Guid.NewGuid());
            var graphUser = new GraphUser { Id = userId.Value.ToString(), DisplayName = null! };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(graphUser)
                });

            // Act
            var result = await _service.GetUserAsync(userId);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Id.Value).IsEqualTo(userId.Value);
            await Assert.That(result.DisplayName).IsNull();
        }
    }

    public class SearchUsersAsyncMethod : GraphUserServiceTests
    {
        [Test]
        public async Task SendsCorrectSearchQuery_WhenSearchingUsers()
        {
            // Arrange
            var query = "Limpert";
            var expectedResponse = new { value = Array.Empty<GraphUser>() };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(expectedResponse)
                });

            // Act
            await _service.SearchUsersAsync(query);

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"$search=\"displayName:{query}\" OR \"mail:{query}\" OR \"givenName:{query}\" OR \"surname:{query}\"") &&
                    req.Headers.Contains("ConsistencyLevel") &&
                    req.Headers.GetValues("ConsistencyLevel").First() == "eventual"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task ReturnsMatchedUsers_WhenSearchSucceeds()
        {
            // Arrange
            var query = "test";
            var expectedUsers = new[]
            {
                new GraphUser { Id = "1", DisplayName = "Test User", Mail = "test@example.com" },
                new GraphUser { Id = "2", DisplayName = "Another Test", Mail = "test2@example.com" }
            };
            var response = new { value = expectedUsers };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });

            // Act
            var result = await _service.SearchUsersAsync(query);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Count()).IsEqualTo(2);
            await Assert.That(result.First().DisplayName).IsEqualTo("Test User");
            await Assert.That(result.Last().DisplayName).IsEqualTo("Another Test");
        }

        [Test]
        public async Task ReturnsEmptyArray_WhenNoMatchesFound()
        {
            // Arrange
            var query = "nonexistent";
            var response = new { value = Array.Empty<GraphUser>() };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });

            // Act
            var result = await _service.SearchUsersAsync(query);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsEmpty();
        }

        [Test]
        public async Task ReturnsEmptyArray_WhenResponseIsNull()
        {
            // Arrange
            var query = "test";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create<object?>(null)
                });

            // Act
            var result = await _service.SearchUsersAsync(query);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsEmpty();
        }

        [Test]
        public async Task ThrowsHttpRequestException_WhenApiReturnsError()
        {
            // Arrange
            var query = "test";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"Invalid search query\"}}")
                });

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await _service.SearchUsersAsync(query);
            });
        }
    }
}
