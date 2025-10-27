using api.Extensions;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using ZgM.ProjectCoordinator.Shared;

namespace ZgM.Projectcoordinator.api
{
    public class GetPlaces(ILogger<GetPlaces> logger, IPlaceService placeService)
    {
        [Function(nameof(GetPlaces))]
        [ProducesResponseType(200, Type = typeof(Place[]))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userId}/places")] HttpRequest req, string userId)
        {
            using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetPlaces) } }))
            {
                var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
                if (!authenticationStatus)
                    return authenticationResponse!;

                var (isValid, errorResult, authenticatedUserId) = req.ValidateUserAccess(userId, logger);
                if (!isValid)
                    return errorResult!;

                logger.LogTrace($"{nameof(GetPlaces)} invoked for user {authenticatedUserId}");
                var places = await placeService.GetPlacesByUserIdAsync(authenticatedUserId);
                logger.LogDebug("Returning {Count} places for user {UserId}", places.Count(), authenticatedUserId);
                return new OkObjectResult(places.ToArray());
            }
        }
    }
}
