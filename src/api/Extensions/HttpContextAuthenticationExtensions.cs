using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

namespace api.Extensions;

public static class HttpContextAuthenticationExtensions
{
    /// <summary>
    /// Authenticates the Azure Function request and validates required scopes and roles.
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="scopes">Optional array of required scopes. User must have at least one of these scopes.</param>
    /// <param name="roles">Optional array of required roles. User must have at least one of these roles.</param>
    /// <returns>A tuple indicating authentication success and an optional response for failed authentication</returns>
    public static async Task<(bool IsAuthenticated, IActionResult? Response)> AuthorizeAzureFunctionAsync(
        this HttpContext httpContext,
        string[]? scopes = null,
        string[]? roles = null)
    {
        var (authenticationStatus, authenticationResponse) = await httpContext.AuthenticateAzureFunctionAsync();
        
        if (!authenticationStatus)
        {
            return (false, authenticationResponse);
        }

        // Validate scopes if provided
        if (scopes is { Length: > 0 })
        {
            try
            {
                httpContext.VerifyUserHasAnyAcceptedScope(scopes);
            }
            catch (Exception)
            {
                return (false, new UnauthorizedObjectResult(new ProblemDetails
                {
                    Title = "Insufficient Scope",
                    Detail = $"Required scope: {string.Join(" or ", scopes)}",
                    Status = StatusCodes.Status403Forbidden
                }));
            }
        }

        // Validate roles if provided
        if (roles is { Length: > 0 })
        {
            try
            {
                httpContext.ValidateAppRole(roles);
            }
            catch (Exception)
            {
                return (false, new UnauthorizedObjectResult(new ProblemDetails
                {
                    Title = "Insufficient Role",
                    Detail = $"Required role: {string.Join(" or ", roles)}",
                    Status = StatusCodes.Status403Forbidden
                }));
            }
        }

        return (true, null);
    }
}
