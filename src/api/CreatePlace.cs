using api.Exceptions;
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
    public class CreatePlace(ILogger<CreatePlace> logger, IPlaceService placeService)
    {
        [Function(nameof(CreatePlace))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/{userId}/places")] HttpRequest req,
            string userId)
        {
            using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(CreatePlace) } }))
            {
                await req.HttpContext.AuthorizeAzureFunctionAsync(scopes: ["Places.Create"]);

                var authenticatedUserId = req.HttpContext.User.GetObjectId();
                if (authenticatedUserId == null)
                {
                    return new UnauthorizedObjectResult("Unable to determine authenticated user");
                }

                var requestedUserId = UserId.Parse(userId);

                // Check authorization: user must be creating for themselves OR have admin role
                if (authenticatedUserId != requestedUserId.Value.ToString() &&
                    !req.HttpContext.User.IsInRole("admin"))
                {
                    return new ForbidResult("Only admins can create places for other users");
                }

                var sharedRequest = await req.ReadFromJsonAsync<ZgM.ProjectCoordinator.Shared.PlaceRequest>();

                // Create API PlaceRequest with userId from route
                var placeRequest = Models.PlaceRequest.FromShared(sharedRequest, requestedUserId);
                var place = await placeService.AddPlace(placeRequest);

                return new CreatedResult($"/users/{userId}/places/{place.Id}", place);
            }
        }
    }
}
