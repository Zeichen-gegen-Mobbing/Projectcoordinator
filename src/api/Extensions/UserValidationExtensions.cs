using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;

namespace api.Extensions
{
    public static class UserValidationExtensions
    {
        public static (bool isValid, IActionResult? errorResult, UserId userId) ValidateUserAccess(
            this HttpRequest request,
            string requestedUserId,
            ILogger logger)
        {
            var userIdClaim = request.HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (userIdClaim == null)
            {
                logger.LogError("User ID claim not found");
                return (false, new UnauthorizedResult(), default);
            }

            if (!UserId.TryParse(userIdClaim.Value, default, out var authenticatedUserId))
            {
                logger.LogError("User ID claim is not a valid GUID: {ClaimValue}", userIdClaim.Value);
                return (false, new UnauthorizedResult(), default);
            }

            if (!UserId.TryParse(requestedUserId, default, out var requestedUserIdValue))
            {
                logger.LogWarning("Requested user ID is not a valid GUID: {RequestedUserId}", requestedUserId);
                return (false, new BadRequestObjectResult("Invalid user ID format"), default);
            }

            if (authenticatedUserId.Value != requestedUserIdValue.Value)
            {
                logger.LogWarning("User {AuthenticatedUserId} attempted to access resources for user {RequestedUserId}",
                    authenticatedUserId, requestedUserIdValue);
                return (false, new ForbidResult(), default);
            }

            return (true, null, authenticatedUserId);
        }
    }
}
