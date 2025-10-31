using api.Extensions;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using ZgM.ProjectCoordinator.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace api
{
    public class GetPlaces(ILogger<GetPlaces> logger, IPlaceService placeService)
    {
        [Function(nameof(GetPlaces))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{userId}/places")] HttpRequest req,
            string userId)
        {
            using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetPlaces) } }))
            {

                // Require Places.Read scope
                await req.HttpContext.AuthorizeAzureFunctionAsync(scopes: ["Places.Read"]);

                // Get authenticated user ID
                var authenticatedUserId = req.HttpContext.User.GetObjectId();
                if (authenticatedUserId == null)
                {
                    return new UnauthorizedObjectResult("Unable to determine authenticated user");
                }

                var requestedUserId = UserId.Parse(userId);

                // Check authorization: user must be reading their own places OR have admin role
                if (authenticatedUserId != requestedUserId.Value.ToString())
                {
                    // User is trying to read someone else's places - must be admin
                    var isAdmin = req.HttpContext.User.IsInRole("admin");
                    if (!isAdmin)
                    {
                        return new ForbidResult("Only admins can read other users' places");
                    }
                }

                var places = await placeService.GetPlacesAsync(requestedUserId);

                // Return places without location data - Place type only contains name and IDs
                return new OkObjectResult(places.ToList());
            }
        }
    }
}
