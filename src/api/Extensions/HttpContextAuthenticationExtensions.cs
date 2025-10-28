using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Globalization;
using System.Security.Authentication;
using System.Security.Claims;

namespace api.Extensions;

public static class HttpContextAuthenticationExtensions
{
    /// <summary>
    /// Authenticates the Azure Function request and validates required scopes and roles.
    /// Throws AuthenticationException if authentication fails.
    /// Throws UnauthorizedAccessException if scope or role validation fails.
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="scopes">Optional array of required scopes. User must have at least one of these scopes.</param>
    /// <param name="roles">Optional array of required roles. User must have at least one of these roles.</param>
    /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when scope or role validation fails</exception>
    public static async Task AuthorizeAzureFunctionAsync(
        this HttpContext httpContext,
        string[]? scopes = null,
        string[]? roles = null)
    {
        await AuthenticateAzureFunctionAsync(httpContext);

        // Validate scopes if provided - this will throw if validation fails
        if (scopes is { Length: > 0 })
        {
            VerifyUserHasAnyAcceptedScope(httpContext, scopes);
        }

        // Validate roles if provided - this will throw if validation fails
        if (roles is { Length: > 0 })
        {
            ValidateAppRole(httpContext, roles);
        }
    }

    /// <summary>
    /// Enables an Azure Function to act as/expose a protected web API, enabling bearer token authentication.
    /// Calling this method from your Azure function validates the token and exposes the identity of the user or app on behalf of which your function is called,
    /// in the HttpContext.User member, where your function can make use of it.
    /// Based on <see cref="AzureFunctionsAuthenticationHttpContextExtension.AuthenticateAzureFunctionAsync"/> to throw AuthenticationException instead of returning AuthenticateResult.
    /// </summary>
    /// <param name="httpContext">The current HTTP Context, such as req.HttpContext.</param>
    /// <returns>Throws <see cref="AuthenticationException"/> if authentication fails.</returns>
    private static async Task AuthenticateAzureFunctionAsync(
        HttpContext httpContext)
    {
        AuthenticateResult result =
            await httpContext.AuthenticateAsync(Constants.Bearer).ConfigureAwait(false);

        if (result != null && result.Succeeded)
        {
            httpContext.User = result.Principal!;
        }
        else
        {
            throw new AuthenticationException(result?.Failure?.Message);
        }
    }

    /// <summary>
    /// When applied to an <see cref="HttpContext"/>, verifies that the user authenticated in the
    /// web API has any of the accepted scopes.
    /// If the authenticated user does not have any of these <paramref name="acceptedScopes"/>, the
    /// method throws a <see cref="UnauthorizedAccessException"/> with a message indicating the missing scopes.
    /// Based on <see cref="ScopesRequiredHttpContextExtensions.VerifyUserHasAnyAcceptedScope"/> as modified to throw exception and not write to context.
    /// </summary>
    /// <param name="context">HttpContext (from the controller).</param>
    /// <param name="acceptedScopes">Scopes accepted by this web API.</param>
    private static void VerifyUserHasAnyAcceptedScope(HttpContext context, params string[] acceptedScopes)
    {
        IEnumerable<Claim> userClaims;
        ClaimsPrincipal user;

        // Need to lock due to https://learn.microsoft.com/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
        lock (context)
        {
            user = context.User;
            userClaims = user.Claims;
        }

        if (userClaims is null || !userClaims.Any())
        {
            throw new UnauthorizedAccessException("The user seems unauthenticated. The HttpContext does not contain any claims.");
        }
        else
        {
            // Attempt with Scp claim
            Claim? scopeClaim = user.FindFirst(ClaimConstants.Scp);

            // Fallback to Scope claim name
            scopeClaim ??= user.FindFirst(ClaimConstants.Scope);

            if (scopeClaim is null || !scopeClaim.Value.Split(' ').Intersect(acceptedScopes).Any())
            {
                string message = string.Format(CultureInfo.InvariantCulture, "The 'scope' or 'scp' claim does not contain scopes '{0}' or was not found.", string.Join(",", acceptedScopes));

                throw new UnauthorizedAccessException(message);
            }
        }
    }

    /// <summary>
    /// When applied to an <see cref="HttpContext"/>, verifies that the application
    /// has the expected roles.
    /// Based on <see cref="RolesRequiredHttpContextExtensions.ValidateAppRole"/> as modified to throw exception and not write to context.
    /// </summary>
    /// <param name="context">HttpContext (from the controller).</param>
    /// <param name="acceptedRoles">Roles accepted by this web API.</param>
    /// <remarks>When the roles don't match, an <see cref="UnauthorizedAccessException"/> is thrown.</remarks>
    private static void ValidateAppRole(HttpContext context, params string[] acceptedRoles)
    {
        IEnumerable<Claim> userClaims;
        ClaimsPrincipal user;

        // Need to lock due to https://learn.microsoft.com/aspnet/core/performance/performance-best-practices?#do-not-access-httpcontext-from-multiple-threads
        lock (context)
        {
            user = context.User;
            userClaims = user.Claims;
        }

        // TODO: check this logic.
        if (userClaims is null || !userClaims.Any())
        {
            throw new UnauthorizedAccessException("The user seems unauthenticated. The HttpContext does not contain any claims.");
        }
        else
        {
            // Attempt with Roles claim
            IEnumerable<string> rolesClaim = userClaims.Where(
                c => c.Type == ClaimConstants.Roles || c.Type == ClaimConstants.Role)
                .SelectMany(c => c.Value.Split(' '));

            if (!rolesClaim.Intersect(acceptedRoles).Any())
            {
                string message = string.Format(CultureInfo.InvariantCulture, "The 'roles' or 'role' claim does not contain roles '{0}' or was not found.", string.Join(", ", acceptedRoles));

                throw new UnauthorizedAccessException(message);
            }
        }
    }
}
