using api.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Middleware;

public class AuthorizationHeaderMiddlewareTests
{
    public class ReplaceAuthorizationHeaderMethod
    {
        [Test]
        public async Task DoesNothingWhenHeadersAreNull()
        {
            // Arrange
            HttpHeadersCollection? headers = null;

            // Act
            AuthorizationHeaderMiddleware.ReplaceAuthorizationHeader(headers);

            // Assert
            await Assert.That(headers).IsNull();
        }

        [Test]
        public async Task OverwritesExistingAuthorizationHeader()
        {
            // Arrange
            var headers = new HttpHeadersCollection
            {
                { CustomHttpHeaders.SwaAuthorization, "Bearer test-token-from-swa" },
                { "Authorization", "Bearer original-token" }
            };

            // Act
            AuthorizationHeaderMiddleware.ReplaceAuthorizationHeader(headers);

            // Assert
            await Assert.That(headers.GetValues("Authorization").First()).IsEqualTo("Bearer test-token-from-swa");
        }

        [Test]
        public async Task AddsAuthorizationHeaderWhenMissing()
        {
            // Arrange
            var swaAuthValue = "Bearer test-token-from-swa";
            var headers = new HttpHeadersCollection
            {
                { CustomHttpHeaders.SwaAuthorization, swaAuthValue }
            };

            // Act
            AuthorizationHeaderMiddleware.ReplaceAuthorizationHeader(headers);

            // Assert
            await Assert.That(headers.Contains("Authorization")).IsTrue();
            await Assert.That(headers.GetValues("Authorization").First()).IsEqualTo(swaAuthValue);
        }

        /// <summary>
        /// Given no ZgM-SWA-Authorization header present
        /// When ReplaceAuthorizationHeader is called
        /// Then the existing Authorization header should remain unchanged
        /// </summary>
        [Test]
        public async Task PreservesAuthorizationHeaderWhenSwaHeaderMissing()
        {
            // Arrange
            var authorizationValue = "Bearer original-token";
            var headers = new HttpHeadersCollection
            {
                { "Authorization", authorizationValue }
            };

            // Act
            AuthorizationHeaderMiddleware.ReplaceAuthorizationHeader(headers);

            // Assert
            await Assert.That(headers.GetValues("Authorization").First()).IsEqualTo(authorizationValue);
        }
    }
}
