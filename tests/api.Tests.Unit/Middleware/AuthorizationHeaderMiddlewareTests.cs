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
        public async Task OverwritesExistingAuthorizationHeader()
        {
            // Arrange
            var headers = new HttpHeadersCollection();
            
            headers.Add(CustomHttpHeaders.SwaAuthorization, "Bearer test-token-from-swa");
            headers.Add("Authorization", "Bearer original-token");
            
            // Act
            AuthorizationHeaderMiddleware.ReplaceAuthorizationHeader(headers);
            
            // Assert
            await Assert.That(headers.GetValues("Authorization").First()).IsEqualTo("Bearer test-token-from-swa");
        }
        
        [Test]
        public async Task AddsAuthorizationHeaderWhenMissing()
        {
            // Arrange
            var headers = new HttpHeadersCollection();
            
            headers.Add(CustomHttpHeaders.SwaAuthorization, "Bearer test-token-from-swa");
            
            // Act
            AuthorizationHeaderMiddleware.ReplaceAuthorizationHeader(headers);
            
            // Assert
            await Assert.That(headers.Contains("Authorization")).IsTrue();
            await Assert.That(headers.GetValues("Authorization").First()).IsEqualTo("Bearer test-token-from-swa");
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
            var headers = new HttpHeadersCollection();
            
            headers.Add("Authorization", "Bearer original-token");
            
            // Act
            AuthorizationHeaderMiddleware.ReplaceAuthorizationHeader(headers);
            
            // Assert
            await Assert.That(headers.GetValues("Authorization").First()).IsEqualTo("Bearer original-token");
        }
    }
}
