using System.Security.Claims;
using api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Extensions;

public class UserValidationExtensionsTests
{
    public class ValidateUserAccess
    {
        [Test]
        public async Task ReturnsUnauthorized_WhenUserIdClaimNotFound()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            var request = httpContext.Request;
            var requestedUserId = Guid.NewGuid().ToString();

            // Act
            var (isValid, errorResult, userId) = request.ValidateUserAccess(requestedUserId, mockLogger.Object);

            // Assert
            await Assert.That(isValid).IsFalse();
            await Assert.That(errorResult).IsTypeOf<UnauthorizedResult>();
            await Assert.That(userId).IsEqualTo(default(UserId));
        }

        [Test]
        public async Task ReturnsUnauthorized_WhenUserIdClaimIsNotValidGuid()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "not-a-guid")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var request = httpContext.Request;
            var requestedUserId = Guid.NewGuid().ToString();

            // Act
            var (isValid, errorResult, userId) = request.ValidateUserAccess(requestedUserId, mockLogger.Object);

            // Assert
            await Assert.That(isValid).IsFalse();
            await Assert.That(errorResult).IsTypeOf<UnauthorizedResult>();
            await Assert.That(userId).IsEqualTo(default(UserId));
        }

        [Test]
        public async Task ReturnsBadRequest_WhenRequestedUserIdIsNotValidGuid()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var authenticatedUserId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", authenticatedUserId.ToString())
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var request = httpContext.Request;
            var requestedUserId = "not-a-guid";

            // Act
            var (isValid, errorResult, userId) = request.ValidateUserAccess(requestedUserId, mockLogger.Object);

            // Assert
            await Assert.That(isValid).IsFalse();
            await Assert.That(errorResult).IsTypeOf<BadRequestObjectResult>();
            await Assert.That(userId).IsEqualTo(default(UserId));
            
            var badRequestResult = errorResult as BadRequestObjectResult;
            await Assert.That(badRequestResult!.Value).IsEqualTo("Invalid user ID format");
        }

        [Test]
        public async Task ReturnsForbidden_WhenAuthenticatedUserDoesNotMatchRequestedUser()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var authenticatedUserId = Guid.NewGuid();
            var requestedUserId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", authenticatedUserId.ToString())
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var request = httpContext.Request;

            // Act
            var (isValid, errorResult, userId) = request.ValidateUserAccess(requestedUserId.ToString(), mockLogger.Object);

            // Assert
            await Assert.That(isValid).IsFalse();
            await Assert.That(errorResult).IsTypeOf<ForbidResult>();
            await Assert.That(userId).IsEqualTo(default(UserId));
        }

        [Test]
        public async Task ReturnsSuccess_WhenAuthenticatedUserMatchesRequestedUser()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var userId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId.ToString())
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var request = httpContext.Request;

            // Act
            var (isValid, errorResult, returnedUserId) = request.ValidateUserAccess(userId.ToString(), mockLogger.Object);

            // Assert
            await Assert.That(isValid).IsTrue();
            await Assert.That(errorResult).IsNull();
            await Assert.That(returnedUserId.Value).IsEqualTo(userId);
        }

        /// <summary>
        /// Given authenticated user matches requested user
        /// When ValidateUserAccess is called
        /// Then should return the authenticated UserId for use in business logic
        /// </summary>
        [Test]
        public async Task ReturnsAuthenticatedUserId_WhenValidationSucceeds()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var expectedUserId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", expectedUserId.ToString())
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var request = httpContext.Request;

            // Act
            var (isValid, errorResult, userId) = request.ValidateUserAccess(expectedUserId.ToString(), mockLogger.Object);

            // Assert
            await Assert.That(isValid).IsTrue();
            await Assert.That(userId).IsEqualTo(new UserId(expectedUserId));
        }

        [Test]
        public void LogsError_WhenUserIdClaimNotFound()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            var request = httpContext.Request;

            // Act
            request.ValidateUserAccess(Guid.NewGuid().ToString(), mockLogger.Object);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User ID claim not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void LogsWarning_WhenUserTriesToAccessAnotherUsersResources()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var authenticatedUserId = Guid.NewGuid();
            var requestedUserId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            var claims = new[]
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", authenticatedUserId.ToString())
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var request = httpContext.Request;

            // Act
            request.ValidateUserAccess(requestedUserId.ToString(), mockLogger.Object);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to access resources")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
