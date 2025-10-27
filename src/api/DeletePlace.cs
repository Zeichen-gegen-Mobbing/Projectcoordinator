using api.Extensions;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using ZgM.ProjectCoordinator.Shared;

namespace api
{
    public class DeletePlace
    {
        private readonly ILogger<DeletePlace> _logger;
        private readonly IPlaceService _placeService;

        public DeletePlace(ILogger<DeletePlace> logger, IPlaceService placeService)
        {
            _logger = logger;
            _placeService = placeService;
        }

        [Function(nameof(DeletePlace))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "users/{userId}/places/{placeId}")] HttpRequest request, string userId, string placeId)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(DeletePlace) } }))
            {
                var (authenticationStatus, authenticationResponse) = await request.HttpContext.AuthenticateAzureFunctionAsync();
                if (!authenticationStatus)
                    return authenticationResponse!;

                var (isValid, errorResult, authenticatedUserId) = request.ValidateUserAccess(userId, _logger);
                if (!isValid)
                    return errorResult!;

                _logger.LogInformation("Deleting place {PlaceId} for user {UserId}", placeId, authenticatedUserId);
                
                await _placeService.DeletePlace(new PlaceId(placeId), authenticatedUserId);
                return new NoContentResult();
            }
        }
    }
}
