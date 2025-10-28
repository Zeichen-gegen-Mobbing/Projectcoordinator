using api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Moq;
using System.Security.Authentication;
using System.Security.Claims;
using TUnit.Assertions.Extensions;

namespace api.Tests.Unit.Extensions;

public class HttpContextAuthenticationExtensionsTests
{
    public class AuthorizeAzureFunctionAsyncMethod
    {
        [Test]
        public async Task ThrowsAuthenticationException_WhenAuthenticationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: false);

            // Act & Assert
            await Assert.ThrowsAsync<AuthenticationException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync());
        }

        [Test]
        public async Task Succeeds_WhenAuthenticationSucceedsWithoutScopesAndRoles()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: true);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(scopes: null, roles: null);
        }

        [Test]
        public async Task Succeeds_WhenAuthenticationSucceedsWithValidScope()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"]);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(scopes: ["Trips.Calculate"]);
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenScopeValidationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Other.Scope"]);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(scopes: ["Trips.Calculate"]));

            await Assert.That(exception!.Message).Contains("Trips.Calculate");
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenNoScopesInClaims()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(scopes: ["Trips.Calculate"]));

            // When no claims exist, the validation detects user as unauthenticated
            await Assert.That(exception!.Message).Contains("unauthenticated");
        }

        [Test]
        public async Task Succeeds_WhenAuthenticationSucceedsWithValidRole()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                roles: ["projectcoordination"]);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(roles: ["projectcoordination"]);
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenRoleValidationFails()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                roles: ["other-role"]);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(roles: ["projectcoordination"]));

            await Assert.That(exception!.Message).Contains("projectcoordination");
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenNoRolesInClaims()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(isAuthenticated: true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(roles: ["projectcoordination"]));

            // When no claims exist, the validation detects user as unauthenticated
            await Assert.That(exception!.Message).Contains("unauthenticated");
        }

        [Test]
        public async Task Succeeds_WhenBothScopeAndRoleAreValid()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"],
                roles: ["projectcoordination"]);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Trips.Calculate"],
                roles: ["projectcoordination"]);
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenScopeIsValidButRoleIsNot()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"],
                roles: ["other-role"]);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(
                    scopes: ["Trips.Calculate"],
                    roles: ["projectcoordination"]));

            await Assert.That(exception!.Message).Contains("role");
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenRoleIsValidButScopeIsNot()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Other.Scope"],
                roles: ["projectcoordination"]);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(
                    scopes: ["Trips.Calculate"],
                    roles: ["projectcoordination"]));

            await Assert.That(exception!.Message).Contains("scope");
        }

        [Test]
        public async Task Succeeds_WhenUserHasOneOfMultipleRequiredScopes()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                scopes: ["Trips.Calculate"]);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(scopes: ["Trips.Calculate", "Trips.Read"]);
        }

        [Test]
        public async Task Succeeds_WhenUserHasMultipleScopesInSingleClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimConstants.Scp, "Trips.Calculate Trips.Read Places.Create")
            };
            var httpContext = CreateMockHttpContext(isAuthenticated: true, claims: claims);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(scopes: ["Places.Create"]);
        }

        [Test]
        public async Task Succeeds_WhenUserHasOneOfMultipleRequiredRoles()
        {
            // Arrange
            var httpContext = CreateMockHttpContext(
                isAuthenticated: true,
                roles: ["projectcoordination"]);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(roles: ["projectcoordination", "admin"]);
        }

        [Test]
        public async Task Succeeds_WhenUserHasMultipleRolesInSingleClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimConstants.Roles, "projectcoordination admin")
            };
            var httpContext = CreateMockHttpContext(isAuthenticated: true, claims: claims);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(roles: ["admin"]);
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

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(scopes: []);
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

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(roles: []);
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenUserHasNoClaimsAndScopeRequired()
        {
            // Arrange
            var claims = new List<Claim>(); // Empty claims
            var httpContext = CreateMockHttpContext(isAuthenticated: true, claims: claims);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(scopes: ["Trips.Calculate"]));

            await Assert.That(exception!.Message).Contains("unauthenticated");
        }

        [Test]
        public async Task ThrowsUnauthorizedAccessException_WhenUserHasNoClaimsAndRoleRequired()
        {
            // Arrange
            var claims = new List<Claim>(); // Empty claims
            var httpContext = CreateMockHttpContext(isAuthenticated: true, claims: claims);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await httpContext.AuthorizeAzureFunctionAsync(roles: ["projectcoordination"]));

            await Assert.That(exception!.Message).Contains("unauthenticated");
        }

        [Test]
        public async Task Succeeds_WhenUsingScopeClaimInsteadOfScp()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimConstants.Scope, "Trips.Calculate")
            };
            var httpContext = CreateMockHttpContext(isAuthenticated: true, claims: claims);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(scopes: ["Trips.Calculate"]);
        }

        [Test]
        public async Task Succeeds_WhenUsingRoleClaimInsteadOfRoles()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimConstants.Role, "projectcoordination")
            };
            var httpContext = CreateMockHttpContext(isAuthenticated: true, claims: claims);

            // Act & Assert - should not throw
            await httpContext.AuthorizeAzureFunctionAsync(roles: ["projectcoordination"]);
        }
    }

    private static HttpContext CreateMockHttpContext(
        bool isAuthenticated,
        string[]? scopes = null,
        string[]? roles = null,
        List<Claim>? claims = null)
    {
        var claimsList = claims ?? new List<Claim>();

        if (scopes != null)
        {
            foreach (var scope in scopes)
            {
                claimsList.Add(new Claim(ClaimConstants.Scp, scope));
            }
        }

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claimsList.Add(new Claim(ClaimConstants.Roles, role));
            }
        }

        var identity = new ClaimsIdentity(claimsList, isAuthenticated ? "TestAuth" : null);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        // Mock the authentication service
        var authServiceMock = new Mock<IAuthenticationService>();
        var authResult = isAuthenticated
            ? AuthenticateResult.Success(new AuthenticationTicket(principal, Constants.Bearer))
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
