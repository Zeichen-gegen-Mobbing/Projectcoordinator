using api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Moq;
using System.Security.Claims;
using TUnit.Assertions.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests.Unit.Extensions;

public class HttpContextAuthenticationExtensionsTests
{
    public class AuthorizeAzureFunctionAsyncMethod
    {
        [Test]
        public async Task ReturnsUnauthorized_WhenAuthenticationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: false);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync();

            // Assert
            await Assert.That(isAuthenticated).IsFalse();
            await Assert.That(response).IsNotNull();
        }

        [Test]
        public async Task ReturnsSuccess_WhenAuthenticationSucceedsWithoutScopesAndRoles()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: true);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: null,
                roles: null);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        [Test]
        public async Task ReturnsSuccess_WhenAuthenticationSucceedsWithValidScope()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate"]);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        [Test]
        public async Task ReturnsForbidden_WhenScopeValidationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Other.Scope"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate"]);

            // Assert
            await Assert.That(isAuthenticated).IsFalse();
            await Assert.That(response).IsNotNull();
            await Assert.That(response).IsTypeOf<UnauthorizedObjectResult>();

            var objectResult = (UnauthorizedObjectResult)response!;
            await Assert.That(objectResult.Value).IsTypeOf<ProblemDetails>();

            var problemDetails = (ProblemDetails)objectResult.Value!;
            await Assert.That(problemDetails.Title).IsEqualTo("Insufficient Scope");
            await Assert.That(problemDetails.Status).IsEqualTo(StatusCodes.Status403Forbidden);
        }

        [Test]
        public async Task ReturnsSuccess_WhenAuthenticationSucceedsWithValidRole()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                roles: ["projectcoordination"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                roles: ["projectcoordination"]);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        [Test]
        public async Task ReturnsForbidden_WhenRoleValidationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                roles: ["other-role"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                roles: ["projectcoordination"]);

            // Assert
            await Assert.That(isAuthenticated).IsFalse();
            await Assert.That(response).IsNotNull();
            await Assert.That(response).IsTypeOf<UnauthorizedObjectResult>();

            var objectResult = (UnauthorizedObjectResult)response!;
            await Assert.That(objectResult.Value).IsTypeOf<ProblemDetails>();

            var problemDetails = (ProblemDetails)objectResult.Value!;
            await Assert.That(problemDetails.Title).IsEqualTo("Insufficient Role");
            await Assert.That(problemDetails.Status).IsEqualTo(StatusCodes.Status403Forbidden);
        }

        [Test]
        public async Task ReturnsSuccess_WhenBothScopeAndRoleAreValid()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"],
                roles: ["projectcoordination"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate"],
                roles: ["projectcoordination"]);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        [Test]
        public async Task ReturnsForbidden_WhenScopeIsValidButRoleIsNot()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"],
                roles: ["other-role"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate"],
                roles: ["projectcoordination"]);

            // Assert
            await Assert.That(isAuthenticated).IsFalse();
            await Assert.That(response).IsNotNull();
            await Assert.That(response).IsTypeOf<UnauthorizedObjectResult>();

            var objectResult = (UnauthorizedObjectResult)response!;
            var problemDetails = (ProblemDetails)objectResult.Value!;
            await Assert.That(problemDetails.Title).IsEqualTo("Insufficient Role");
        }

        [Test]
        public async Task ReturnsForbidden_WhenRoleIsValidButScopeIsNot()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Other.Scope"],
                roles: ["projectcoordination"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate"],
                roles: ["projectcoordination"]);

            // Assert
            await Assert.That(isAuthenticated).IsFalse();
            await Assert.That(response).IsNotNull();
            await Assert.That(response).IsTypeOf<UnauthorizedObjectResult>();

            var objectResult = (UnauthorizedObjectResult)response!;
            var problemDetails = (ProblemDetails)objectResult.Value!;
            await Assert.That(problemDetails.Title).IsEqualTo("Insufficient Scope");
        }

        [Test]
        public async Task ReturnsSuccess_WhenUserHasOneOfMultipleRequiredScopes()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate", "Trips.Read"]);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        [Test]
        public async Task ReturnsSuccess_WhenUserHasOneOfMultipleRequiredRoles()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                roles: ["projectcoordination"]);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                roles: ["projectcoordination", "admin"]);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        /// <summary>
        /// Given: Empty scopes array provided
        /// When: AuthorizeAzureFunctionAsync is called
        /// Then: No scope validation occurs and authentication succeeds
        /// </summary>
        [Test]
        public async Task SkipsScopeValidation_WhenEmptyScopesArrayProvided()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: true);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: []);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        /// <summary>
        /// Given: Empty roles array provided
        /// When: AuthorizeAzureFunctionAsync is called
        /// Then: No role validation occurs and authentication succeeds
        /// </summary>
        [Test]
        public async Task SkipsRoleValidation_WhenEmptyRolesArrayProvided()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: true);

            // Act
            var (isAuthenticated, response) = await httpContext.AuthorizeAzureFunctionAsync(
                roles: []);

            // Assert
            await Assert.That(isAuthenticated).IsTrue();
            await Assert.That(response).IsNull();
        }

        [Test]
        public async Task IncludesRequiredScopeInErrorMessage_WhenScopeValidationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Other.Scope"]);

            // Act
            var (_, response) = await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate"]);

            // Assert
            var objectResult = (UnauthorizedObjectResult)response!;
            var problemDetails = (ProblemDetails)objectResult.Value!;
            await Assert.That(problemDetails.Detail).Contains("Trips.Calculate");
        }

        [Test]
        public async Task IncludesRequiredRoleInErrorMessage_WhenRoleValidationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                roles: ["other-role"]);

            // Act
            var (_, response) = await httpContext.AuthorizeAzureFunctionAsync(
                roles: ["projectcoordination"]);

            // Assert
            var objectResult = (UnauthorizedObjectResult)response!;
            var problemDetails = (ProblemDetails)objectResult.Value!;
            await Assert.That(problemDetails.Detail).Contains("projectcoordination");
        }

        private static HttpContext CreateMockHttpContext(
            bool isAuthenticated,
            string[]? scopes = null,
            string[]? roles = null)
        {
            var claims = new List<Claim>();

            if (scopes != null)
            {
                foreach (var scope in scopes)
                {
                    claims.Add(new Claim("scp", scope));
                }
            }

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            // Mock the authentication service
            var authServiceMock = new Mock<IAuthenticationService>();
            var authResult = isAuthenticated
                ? AuthenticateResult.Success(new AuthenticationTicket(principal, "Bearer"))
                : AuthenticateResult.Fail("Authentication failed");

            authServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .ReturnsAsync(authResult);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            httpContext.RequestServices = serviceProviderMock.Object;

            return httpContext;
        }
    }
}
