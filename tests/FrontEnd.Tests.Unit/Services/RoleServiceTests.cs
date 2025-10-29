using System.Net;
using System.Net.Http.Json;
using FrontEnd.Services;
using Moq;
using Moq.Protected;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Services;

public class RoleServiceTests : IDisposable
{
    protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    protected readonly RoleService _service;
    protected readonly HttpClient _httpClient;

    public RoleServiceTests()
    {
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };

        _service = new RoleService(_httpClient);
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
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(roles)
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
            var result1 = await _service.HasRole("admin");
            var result2 = await _service.HasRole("projectcoordination");
            var result3 = await _service.HasRole("viewer");

            // Assert
            await Assert.That(result1).IsTrue();
            await Assert.That(result2).IsTrue();
            await Assert.That(result3).IsFalse();
            VerifyHttpCallCount(Times.Once());
        }

        [Test]
        public async Task HandlesConcurrentCalls()
        {
            // Arrange
            SetupMockResponse(["admin", "projectcoordination"]);

            // Act - Multiple concurrent calls
            var tasks = new[]
            {
                _service.HasRole("admin"),
                _service.HasRole("projectcoordination"),
                _service.HasRole("viewer"),
                _service.HasRole("admin"),
                _service.HasRole("projectcoordination")
            };

            var results = await Task.WhenAll(tasks);

            // Assert
            await Assert.That(results[0]).IsTrue();
            await Assert.That(results[1]).IsTrue();
            await Assert.That(results[2]).IsFalse();
            await Assert.That(results[3]).IsTrue();
            await Assert.That(results[4]).IsTrue();
            VerifyHttpCallCount(Times.Once());
        }
    }
}
