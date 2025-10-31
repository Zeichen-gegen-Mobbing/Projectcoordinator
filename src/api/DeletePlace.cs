using api.Extensions;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using ZgM.ProjectCoordinator.Shared;
using System.Threading.Tasks;

namespace api
{
    public class DeletePlace(ILogger<DeletePlace> logger, IPlaceService placeService)
    {
        [Function(nameof(DeletePlace))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "users/{userId}/places/{placeId}")] HttpRequest req,
            string userId,
            string placeId)
        {
            using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(DeletePlace) } }))
            {
                // Require Places.Delete scope
                await req.HttpContext.AuthorizeAzureFunctionAsync(scopes: ["Places.Delete"]);

                // Get authenticated user ID
                var authenticatedUserId = req.HttpContext.User.GetObjectId();
                if (authenticatedUserId == null)
                {
                    return new UnauthorizedObjectResult("Unable to determine authenticated user");
                }

                var requestedUserId = UserId.Parse(userId);

                // Check authorization: user must be deleting their own place OR have admin role
                if (authenticatedUserId != requestedUserId.Value.ToString())
                {
                    // User is trying to delete someone else's place - must be admin
                    var isAdmin = req.HttpContext.User.IsInRole("admin");
                    if (!isAdmin)
                    {
                        return new ForbidResult("Only admins can delete other users' places");
                    }
                }

                await placeService.DeletePlace(requestedUserId, new PlaceId(placeId));

                return new NoContentResult();
            }
        }
    }
}
