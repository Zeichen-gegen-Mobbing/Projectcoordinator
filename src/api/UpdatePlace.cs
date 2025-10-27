using api.Exceptions;
using api.Extensions;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using ZgM.ProjectCoordinator.Shared;

namespace api
{
    public class UpdatePlace
    {
        private readonly ILogger<UpdatePlace> _logger;
        private readonly IPlaceService _placeService;

        public UpdatePlace(ILogger<UpdatePlace> logger, IPlaceService placeService)
        {
            _logger = logger;
            _placeService = placeService;
        }

        [Function(nameof(UpdatePlace))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "users/{userId}/places/{placeId}")] HttpRequest request, string userId, string placeId)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(UpdatePlace) } }))
            {
                var (authenticationStatus, authenticationResponse) = await request.HttpContext.AuthenticateAzureFunctionAsync();
                if (!authenticationStatus)
                    return authenticationResponse!;

                var (isValid, errorResult, authenticatedUserId) = request.ValidateUserAccess(userId, _logger);
                if (!isValid)
                    return errorResult!;

                var placeRequest = await request.ReadFromJsonAsync<PlaceRequest>();
                _logger.LogInformation("Updating place {PlaceId} for user {UserId}", placeId, authenticatedUserId);
                try
                {
                    var updatedPlace = await _placeService.UpdatePlace(new PlaceId(placeId), placeRequest);
                    return new OkObjectResult(updatedPlace);
                }
                catch (ProblemDetailsException ex)
                {
                    return new BadRequestObjectResult(ex.ProblemDetails);
                }
            }
        }
    }
}
