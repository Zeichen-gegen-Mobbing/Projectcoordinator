using FrontEnd;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Tests.Unit;

public class CustomAuthorizationMessageHandlerTests : IDisposable
{
    protected readonly Mock<IAccessTokenProvider> _tokenProviderMock = new();
    protected readonly Mock<NavigationManager> _navigationMock = new();
#pragma warning disable TUnit0023 // Suppress warning about IDisposable - disposed in Dispose method
    protected readonly CustomAuthorizationMessageHandler _handler;
    protected readonly HttpMessageInvoker _invoker;
#pragma warning restore TUnit0023
    protected readonly string _authorizedUrl = "https://example.com/api/";
    protected readonly AccessToken _accessToken = new() { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };

    public CustomAuthorizationMessageHandlerTests()
    {
        _handler = new CustomAuthorizationMessageHandler(_tokenProviderMock.Object, _navigationMock.Object);
        _handler.InnerHandler = new TestHttpMessageHandler();
        _handler.ConfigureHandler([_authorizedUrl]);

        _invoker = new HttpMessageInvoker(_handler);

        var tokenResult = new AccessTokenResult(AccessTokenResultStatus.Success, _accessToken, null!, null);

        _tokenProviderMock.Setup(x => x.RequestAccessToken(It.IsAny<AccessTokenRequestOptions>())).ReturnsAsync(tokenResult);
    }

    public class SendAsyncMethod : CustomAuthorizationMessageHandlerTests
    {
        [Test]
        public async Task AddsAuthorizationHeader_WhenRequestUriIsAuthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_authorizedUrl}trips");

            // Act
            await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            await Assert.That(request.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsTrue();
            await Assert.That(request.Headers.GetValues(CustomHttpHeaders.SwaAuthorization).First()).IsEqualTo($"Bearer {_accessToken.Value}");
        }

        [Test]
        public async Task DoesNotAddAuthorizationHeader_WhenRequestUriIsNotAuthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/other/endpoint");

            // Act
            await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            await Assert.That(request.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsFalse();
        }

        [Test]
        public async Task Throws_WhenTokenRequestFails()
        {
            // Arrange
            var tokenResult = new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, null!, "https://login.example.com", null);
            _tokenProviderMock.Setup(x => x.RequestAccessToken(It.IsAny<AccessTokenRequestOptions>())).ReturnsAsync(tokenResult);

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_authorizedUrl}trips");

            // Act
            var act = async () =>
            {
                await _invoker.SendAsync(request, CancellationToken.None);
            };

            // Assert
            await Assert.ThrowsAsync<AccessTokenNotAvailableException>(act);
            await Assert.That(request.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsFalse();
        }

        /// <summary>
        /// Given: Multiple authorized URLs configured
        /// When: SendAsync is called with URLs matching and not matching the list
        /// Then: Authorization header is added only to matching URLs
        /// </summary>
        [Test]
        [Arguments("https://api1.example.com/data", true)]
        [Arguments("https://api2.example.com/api/trips", true)]
        [Arguments("https://other.example.com/data", false)]
        public async Task AddsTokenOnlyToMatchingUrls_WhenMultipleAuthorizedUrls(string url, Boolean expectedToHaveToken)
        {
            // Arrange
            var handler = new CustomAuthorizationMessageHandler(_tokenProviderMock.Object, _navigationMock.Object);
            handler.InnerHandler = new TestHttpMessageHandler();
            handler.ConfigureHandler([
                "https://api1.example.com/",
                "https://api2.example.com/api/"
            ]);

            var client = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            await client.SendAsync(request, CancellationToken.None);

            // Cleanup
            client.Dispose();
            handler.Dispose();

            // Assert
            await Assert.That(request.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsEqualTo(expectedToHaveToken);
        }

        [Test]
        public async Task AddsToken_WhenUrlMatchesPrefix()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_authorizedUrl}v1/trips/123");

            // Act
            await _invoker.SendAsync(request, CancellationToken.None);

            // Assert
            await Assert.That(request.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsTrue();
        }
    }

    public class ConfigureHandlerMethod : CustomAuthorizationMessageHandlerTests
    {
        /// <summary>
        /// Given: Handler not configured with authorized URLs (empty list)
        /// When: SendAsync is called with any URL
        /// Then: InvalidOperationException is thrown
        /// </summary>
        [Test]
        public async Task Throws_WhenNoAuthorizedUrlsConfigured()
        {
            // Arrange
            var handler = new CustomAuthorizationMessageHandler(_tokenProviderMock.Object, _navigationMock.Object)
            {
                InnerHandler = new TestHttpMessageHandler()
            };
            // Not calling ConfigureHandler

            var client = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://any-url.com/endpoint");

            // Act
            async Task act()
            {
                await client.SendAsync(request, CancellationToken.None);
            }
            // Cleanup
            client.Dispose();
            handler.Dispose();

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
            await Assert.That(request.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsFalse();
        }
        [Test]
        public async Task ReturnsHandlerInstance_ForFluentConfiguration()
        {
            // Act
            var result = _handler.ConfigureHandler(["https://example.com/"]);

            // Assert
            await Assert.That(result == _handler).IsTrue();
        }

        [Test]
        public async Task ClearsPreviousUrls_WhenCalledMultipleTimes()
        {
            // Arrange
            var newAuthorizedUri = "https://new.example.com/";
            _handler.ConfigureHandler([newAuthorizedUri]);
            var requestToOld = new HttpRequestMessage(HttpMethod.Get, _authorizedUrl);
            var requestToNew = new HttpRequestMessage(HttpMethod.Get, newAuthorizedUri);

            // Act
            await _invoker.SendAsync(requestToOld, CancellationToken.None);
            await _invoker.SendAsync(requestToNew, CancellationToken.None);

            // Assert
            await Assert.That(requestToOld.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsFalse();
            await Assert.That(requestToNew.Headers.Contains(CustomHttpHeaders.SwaAuthorization)).IsTrue();
        }

        /// <summary>
        /// Given: Handler configured with scopes
        /// When: Request is sent to authorized URL
        /// Then: AccessTokenRequestOptions with scopes is passed to token provider
        /// </summary>
        [Test]
        public async Task PassesScopesToTokenProvider_WhenScopesConfigured()
        {
            // Arrange
            var scopes = new[] { "api://app-id/access", "openid", "profile" };
            AccessTokenRequestOptions? capturedOptions = null;

            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResult = new AccessTokenResult(AccessTokenResultStatus.Success, accessToken, null!, null);

            _tokenProviderMock
                .Setup(x => x.RequestAccessToken(It.IsAny<AccessTokenRequestOptions>()))
                .Callback<AccessTokenRequestOptions>(options => capturedOptions = options)
                .ReturnsAsync(tokenResult);

            var handler = new CustomAuthorizationMessageHandler(_tokenProviderMock.Object, _navigationMock.Object);
            handler.InnerHandler = new TestHttpMessageHandler();
            handler.ConfigureHandler(["https://example.com/api/"], scopes);

            var client = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/data");

            // Act
            await client.SendAsync(request, CancellationToken.None);

            // Assert
            await Assert.That(capturedOptions).IsNotNull();
            await Assert.That(capturedOptions!.Scopes).IsNotNull();
            await Assert.That(capturedOptions.Scopes!.Count()).IsEqualTo(3);
            await Assert.That(capturedOptions.Scopes).Contains("api://app-id/access");
            await Assert.That(capturedOptions.Scopes).Contains("openid");
            await Assert.That(capturedOptions.Scopes).Contains("profile");

            // Cleanup
            client.Dispose();
            handler.Dispose();
        }

        /// <summary>
        /// Given: Handler configured without scopes
        /// When: Request is sent to authorized URL
        /// Then: AccessTokenRequestOptions with empty scopes is passed to token provider
        /// </summary>
        [Test]
        public async Task PassesEmptyScopes_WhenNoScopesConfigured()
        {
            // Arrange
            AccessTokenRequestOptions? capturedOptions = null;

            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResult = new AccessTokenResult(AccessTokenResultStatus.Success, accessToken, null!, null);

            _tokenProviderMock
                .Setup(x => x.RequestAccessToken(It.IsAny<AccessTokenRequestOptions>()))
                .Callback<AccessTokenRequestOptions>(options => capturedOptions = options)
                .ReturnsAsync(tokenResult);

            var handler = new CustomAuthorizationMessageHandler(_tokenProviderMock.Object, _navigationMock.Object);
            handler.InnerHandler = new TestHttpMessageHandler();
            handler.ConfigureHandler(["https://example.com/api/"]);

            var client = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/data");

            // Act
            await client.SendAsync(request, CancellationToken.None);

            // Assert
            await Assert.That(capturedOptions).IsNotNull();
            await Assert.That(capturedOptions!.Scopes).IsNotNull();
            await Assert.That(capturedOptions.Scopes!.Count()).IsEqualTo(0);

            // Cleanup
            client.Dispose();
            handler.Dispose();
        }

        /// <summary>
        /// Given: Handler configured with scopes, then reconfigured with different scopes
        /// When: Request is sent to authorized URL
        /// Then: Latest scopes are used
        /// </summary>
        [Test]
        public async Task ClearsPreviousScopes_WhenReconfigured()
        {
            // Arrange
            AccessTokenRequestOptions? capturedOptions = null;

            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResult = new AccessTokenResult(AccessTokenResultStatus.Success, accessToken, null!, null);

            _tokenProviderMock
                .Setup(x => x.RequestAccessToken(It.IsAny<AccessTokenRequestOptions>()))
                .Callback<AccessTokenRequestOptions>(options => capturedOptions = options)
                .ReturnsAsync(tokenResult);

            var handler = new CustomAuthorizationMessageHandler(_tokenProviderMock.Object, _navigationMock.Object);
            handler.InnerHandler = new TestHttpMessageHandler();
            handler.ConfigureHandler(["https://example.com/api/"], ["old-scope"]);
            handler.ConfigureHandler(["https://example.com/api/"], ["new-scope"]);

            var client = new HttpMessageInvoker(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/data");

            // Act
            await client.SendAsync(request, CancellationToken.None);

            // Assert
            await Assert.That(capturedOptions).IsNotNull();
            await Assert.That(capturedOptions!.Scopes).IsNotNull();
            await Assert.That(capturedOptions.Scopes!.Count()).IsEqualTo(1);
            await Assert.That(capturedOptions.Scopes).Contains("new-scope");
            await Assert.That(capturedOptions.Scopes).DoesNotContain("old-scope");

            // Cleanup
            client.Dispose();
            handler.Dispose();
        }
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _invoker.Dispose();
            _handler.Dispose();
        }
    }
}