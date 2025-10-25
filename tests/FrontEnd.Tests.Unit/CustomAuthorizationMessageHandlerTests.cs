using FrontEnd;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Moq;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit;

public class CustomAuthorizationMessageHandlerTests
{
    public class SendAsync
    {
        /// <summary>
        /// Given: Request URI matches configured authorized URL
        /// When: SendAsync is called
        /// Then: ZgM-SWA-Authorization header is added with Bearer token
        /// </summary>
        [Test]
        public async Task AddsAuthorizationHeader_WhenRequestUriIsAuthorized()
        {
            // Arrange
            var tokenProviderMock = new Mock<IAccessTokenProvider>();
            var navigationMock = new Mock<NavigationManager>();
            var handler = new CustomAuthorizationMessageHandler(tokenProviderMock.Object, navigationMock.Object);
            
            handler.ConfigureHandler(new[] { "https://example.com/api/" });
            
            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResultMock = new Mock<AccessTokenResult>();
            tokenResultMock.Setup(x => x.Status).Returns(AccessTokenResultStatus.Success);
            tokenResultMock.Setup(x => x.TryGetToken(out It.Ref<AccessToken?>.IsAny))
                .Returns(new TryGetTokenDelegate((out AccessToken? token) =>
                {
                    token = accessToken;
                    return true;
                }));
            
            tokenProviderMock.Setup(x => x.RequestAccessToken()).ReturnsAsync(tokenResultMock.Object);
            
            handler.InnerHandler = new TestHttpMessageHandler();
            
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/trips");
            
            // Act
            await client.SendAsync(request);
            
            // Assert
            await Assert.That(request.Headers.Contains("ZgM-SWA-Authorization")).IsTrue();
            await Assert.That(request.Headers.GetValues("ZgM-SWA-Authorization").First()).IsEqualTo("Bearer test-token");
        }

        [Test]
        public async Task DoesNotAddAuthorizationHeader_WhenRequestUriIsNotAuthorized()
        {
            // Arrange
            var tokenProviderMock = new Mock<IAccessTokenProvider>();
            var navigationMock = new Mock<NavigationManager>();
            var handler = new CustomAuthorizationMessageHandler(tokenProviderMock.Object, navigationMock.Object);
            
            handler.ConfigureHandler(new[] { "https://example.com/api/" });
            
            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResultMock = new Mock<AccessTokenResult>();
            tokenResultMock.Setup(x => x.Status).Returns(AccessTokenResultStatus.Success);
            tokenResultMock.Setup(x => x.TryGetToken(out It.Ref<AccessToken?>.IsAny))
                .Returns(new TryGetTokenDelegate((out AccessToken? token) =>
                {
                    token = accessToken;
                    return true;
                }));
            
            tokenProviderMock.Setup(x => x.RequestAccessToken()).ReturnsAsync(tokenResultMock.Object);
            
            handler.InnerHandler = new TestHttpMessageHandler();
            
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/other/endpoint");
            
            // Act
            await client.SendAsync(request);
            
            // Assert
            await Assert.That(request.Headers.Contains("ZgM-SWA-Authorization")).IsFalse();
        }

        [Test]
        public async Task DoesNotAddAuthorizationHeader_WhenTokenRequestFails()
        {
            // Arrange
            var tokenProviderMock = new Mock<IAccessTokenProvider>();
            var navigationMock = new Mock<NavigationManager>();
            var handler = new CustomAuthorizationMessageHandler(tokenProviderMock.Object, navigationMock.Object);
            
            handler.ConfigureHandler(new[] { "https://example.com/api/" });
            
            var tokenResultMock = new Mock<AccessTokenResult>();
            tokenResultMock.Setup(x => x.Status).Returns(AccessTokenResultStatus.RequiresRedirect);
            tokenResultMock.Setup(x => x.TryGetToken(out It.Ref<AccessToken?>.IsAny))
                .Returns(new TryGetTokenDelegate((out AccessToken? token) =>
                {
                    token = null;
                    return false;
                }));
            
            tokenProviderMock.Setup(x => x.RequestAccessToken()).ReturnsAsync(tokenResultMock.Object);
            
            handler.InnerHandler = new TestHttpMessageHandler();
            
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/trips");
            
            // Act
            await client.SendAsync(request);
            
            // Assert
            await Assert.That(request.Headers.Contains("ZgM-SWA-Authorization")).IsFalse();
        }

        /// <summary>
        /// Given: Handler not configured with authorized URLs (empty list)
        /// When: SendAsync is called with any URL
        /// Then: Authorization header is added to all requests
        /// </summary>
        [Test]
        public async Task AddsTokenToAllRequests_WhenNoAuthorizedUrlsConfigured()
        {
            // Arrange
            var tokenProviderMock = new Mock<IAccessTokenProvider>();
            var navigationMock = new Mock<NavigationManager>();
            var handler = new CustomAuthorizationMessageHandler(tokenProviderMock.Object, navigationMock.Object);
            
            // Not calling ConfigureHandler, so no authorized URLs
            
            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResultMock = new Mock<AccessTokenResult>();
            tokenResultMock.Setup(x => x.Status).Returns(AccessTokenResultStatus.Success);
            tokenResultMock.Setup(x => x.TryGetToken(out It.Ref<AccessToken?>.IsAny))
                .Returns(new TryGetTokenDelegate((out AccessToken? token) =>
                {
                    token = accessToken;
                    return true;
                }));
            
            tokenProviderMock.Setup(x => x.RequestAccessToken()).ReturnsAsync(tokenResultMock.Object);
            
            handler.InnerHandler = new TestHttpMessageHandler();
            
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://any-url.com/endpoint");
            
            // Act
            await client.SendAsync(request);
            
            // Assert
            await Assert.That(request.Headers.Contains("ZgM-SWA-Authorization")).IsTrue();
            await Assert.That(request.Headers.GetValues("ZgM-SWA-Authorization").First()).IsEqualTo("Bearer test-token");
        }

        /// <summary>
        /// Given: Multiple authorized URLs configured
        /// When: SendAsync is called with URLs matching and not matching the list
        /// Then: Authorization header is added only to matching URLs
        /// </summary>
        [Test]
        public async Task AddsTokenOnlyToMatchingUrls_WhenMultipleAuthorizedUrls()
        {
            // Arrange
            var tokenProviderMock = new Mock<IAccessTokenProvider>();
            var navigationMock = new Mock<NavigationManager>();
            var handler = new CustomAuthorizationMessageHandler(tokenProviderMock.Object, navigationMock.Object);
            
            handler.ConfigureHandler(new[] { 
                "https://api1.example.com/",
                "https://api2.example.com/api/" 
            });
            
            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResultMock = new Mock<AccessTokenResult>();
            tokenResultMock.Setup(x => x.Status).Returns(AccessTokenResultStatus.Success);
            tokenResultMock.Setup(x => x.TryGetToken(out It.Ref<AccessToken?>.IsAny))
                .Returns(new TryGetTokenDelegate((out AccessToken? token) =>
                {
                    token = accessToken;
                    return true;
                }));
            
            tokenProviderMock.Setup(x => x.RequestAccessToken()).ReturnsAsync(tokenResultMock.Object);
            
            handler.InnerHandler = new TestHttpMessageHandler();
            
            var client = new HttpClient(handler);
            
            // Act & Assert - First authorized URL
            var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api1.example.com/data");
            await client.SendAsync(request1);
            await Assert.That(request1.Headers.Contains("ZgM-SWA-Authorization")).IsTrue();
            
            // Act & Assert - Second authorized URL
            var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api2.example.com/api/trips");
            await client.SendAsync(request2);
            await Assert.That(request2.Headers.Contains("ZgM-SWA-Authorization")).IsTrue();
            
            // Act & Assert - Unauthorized URL
            var request3 = new HttpRequestMessage(HttpMethod.Get, "https://other.example.com/data");
            await client.SendAsync(request3);
            await Assert.That(request3.Headers.Contains("ZgM-SWA-Authorization")).IsFalse();
        }

        [Test]
        public async Task AddsToken_WhenUrlMatchesPrefix()
        {
            // Arrange
            var tokenProviderMock = new Mock<IAccessTokenProvider>();
            var navigationMock = new Mock<NavigationManager>();
            var handler = new CustomAuthorizationMessageHandler(tokenProviderMock.Object, navigationMock.Object);
            
            handler.ConfigureHandler(new[] { "https://example.com/api/" });
            
            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResultMock = new Mock<AccessTokenResult>();
            tokenResultMock.Setup(x => x.Status).Returns(AccessTokenResultStatus.Success);
            tokenResultMock.Setup(x => x.TryGetToken(out It.Ref<AccessToken?>.IsAny))
                .Returns(new TryGetTokenDelegate((out AccessToken? token) =>
                {
                    token = accessToken;
                    return true;
                }));
            
            tokenProviderMock.Setup(x => x.RequestAccessToken()).ReturnsAsync(tokenResultMock.Object);
            
            handler.InnerHandler = new TestHttpMessageHandler();
            
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/v1/trips/123");
            
            // Act
            await client.SendAsync(request);
            
            // Assert
            await Assert.That(request.Headers.Contains("ZgM-SWA-Authorization")).IsTrue();
        }

        [Test]
        public async Task MatchesAuthorizedUrl_CaseInsensitive()
        {
            // Arrange
            var tokenProviderMock = new Mock<IAccessTokenProvider>();
            var navigationMock = new Mock<NavigationManager>();
            var handler = new CustomAuthorizationMessageHandler(tokenProviderMock.Object, navigationMock.Object);
            
            handler.ConfigureHandler(new[] { "https://example.com/API/" });
            
            var accessToken = new AccessToken { Value = "test-token", Expires = DateTimeOffset.UtcNow.AddHours(1) };
            var tokenResultMock = new Mock<AccessTokenResult>();
            tokenResultMock.Setup(x => x.Status).Returns(AccessTokenResultStatus.Success);
            tokenResultMock.Setup(x => x.TryGetToken(out It.Ref<AccessToken?>.IsAny))
                .Returns(new TryGetTokenDelegate((out AccessToken? token) =>
                {
                    token = accessToken;
                    return true;
                }));
            
            tokenProviderMock.Setup(x => x.RequestAccessToken()).ReturnsAsync(tokenResultMock.Object);
            
            handler.InnerHandler = new TestHttpMessageHandler();
            
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/trips");
            
            // Act
            await client.SendAsync(request);
            
            // Assert
            await Assert.That(request.Headers.Contains("ZgM-SWA-Authorization")).IsTrue();
        }
    }

    private delegate bool TryGetTokenDelegate(out AccessToken? token);

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
