using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FrontEnd.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using Moq.Protected;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Services;

public class RoleServiceTests : IDisposable
{
    protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    protected readonly Mock<AuthenticationStateProvider> _authStateProviderMock = new();
    protected readonly RoleService _service;
    protected readonly HttpClient _httpClient;

    public RoleServiceTests()
    {
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user123")
        ]));

        _authStateProviderMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(claimsPrincipal));

        _service = new RoleService(_httpClient, _authStateProviderMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetupMockResponse(string[] roles, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(roles);
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });
    }

    private void VerifyHttpCallCount(Times times)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().Contains("user/roles")),
            ItExpr.IsAny<CancellationToken>());
    }

    public class HasRole : RoleServiceTests
    {
        [Test]
        public async Task ReturnsTrue_WhenUserHasRole()
        {
            // Arrange
            SetupMockResponse(["admin", "projectcoordination", "viewer"]);

            // Act
            var result = await _service.HasRole("projectcoordination");

            // Assert
            await Assert.That(result).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_WhenUserDoesNotHaveRole()
        {
            // Arrange
            SetupMockResponse(["admin", "viewer"]);

            // Act
            var result = await _service.HasRole("projectcoordination");

            // Assert
            await Assert.That(result).IsFalse();
        }

        [Test]
        [Arguments("projectcoordination")]
        [Arguments("ProjectCoordination")]
        [Arguments("PROJECTCOORDINATION")]
        public async Task IsCaseInsensitive(string roleName)
        {
            // Arrange
            SetupMockResponse(["ProjectCoordination"]);

            // Act
            var result = await _service.HasRole(roleName);

            // Assert
            await Assert.That(result).IsTrue();
        }

        [Test]
        public async Task ReturnsFalse_WhenRolesArrayIsEmpty()
        {
            // Arrange
            SetupMockResponse([]);

            // Act
            var result = await _service.HasRole("projectcoordination");

            // Assert
            await Assert.That(result).IsFalse();
        }

        [Test]
        public async Task CachesRolesAfterFirstCall()
        {
            // Arrange
            SetupMockResponse(["admin", "projectcoordination"]);

            // Act
            _ = await _service.HasRole("admin");
            _ = await _service.HasRole("projectcoordination");

            // Assert

            VerifyHttpCallCount(Times.Exactly(1));
        }

        [Test]
        public async Task HandlesConcurrentCalls()
        {
            // Arrange
            _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async () =>
            {
                await Task.Delay(50);
                var json = System.Text.Json.JsonSerializer.Serialize<string[]>(["admin", "projectcoordination"]);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

            // Act - Multiple concurrent calls
            var tasks = new[]
            {
                _service.HasRole("admin"),
                _service.HasRole("projectcoordination"),
                _service.HasRole("admin"),
                _service.HasRole("projectcoordination")
            };

            var results = await Task.WhenAll(tasks);

            // Assert
            await Assert.That(results[0]).IsTrue();
            await Assert.That(results[1]).IsTrue();
            await Assert.That(results[2]).IsTrue();
            await Assert.That(results[3]).IsTrue();

            VerifyHttpCallCount(Times.Exactly(1));
        }

        [Test]
        public async Task ReloadsRoles_WhenUserChanges()
        {
            // Arrange
            SetupMockResponse(["admin"]);
            var result1 = await _service.HasRole("admin");

            // Change user
            var newClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "user456")
            ]));
            _authStateProviderMock.Setup(x => x.GetAuthenticationStateAsync())
                .ReturnsAsync(new AuthenticationState(newClaimsPrincipal));

            SetupMockResponse(["admin"]);

            // Act - Second call with different user
            var result2 = await _service.HasRole("admin");

            // Assert
            await Assert.That(result1).IsTrue();
            await Assert.That(result2).IsTrue();

            VerifyHttpCallCount(Times.Exactly(2));
        }

        [Test]
        public async Task RetriesOnce_WhenRoleNotFound()
        {
            // Arrange
            var callCount = 0;
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    string[] roles = callCount == 1 ? ["admin"] : ["admin", "projectcoordination"];
                    var json = System.Text.Json.JsonSerializer.Serialize(roles);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                });

            // Act
            var result = await _service.HasRole("projectcoordination");

            // Assert
            await Assert.That(result).IsTrue();
            VerifyHttpCallCount(Times.Exactly(2));
        }

        [Test]
        public async Task DoesNotRetry_WhenRoleIsFound()
        {
            // Arrange
            SetupMockResponse(["admin", "projectcoordination"]);

            // Act
            var result = await _service.HasRole("projectcoordination");

            // Assert
            await Assert.That(result).IsTrue();
            VerifyHttpCallCount(Times.Once());
        }
    }
}
