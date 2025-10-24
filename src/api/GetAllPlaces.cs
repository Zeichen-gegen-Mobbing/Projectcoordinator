using System.Text.Json;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using ZgM.ProjectCoordinator.Shared;

namespace ZgM.Projectcoordinator.api
{
    public class GetAllPlaces(ILogger<GetAllPlaces> logger, IPlaceService placeService)
    {
        [Function(nameof(GetAllPlaces))]
        [ProducesResponseType(200, Type = typeof(Place[]))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "places")] HttpRequest req)
        {
            using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetAllPlaces) } }))
            {
                var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
                if (!authenticationStatus)
                    return authenticationResponse!;

                logger.LogTrace($"{nameof(GetAllPlaces)} invoked");
                var places = await placeService.GetAllPlacesAsync();
                logger.LogDebug("Returning places: {places}", places);
                return new OkObjectResult(places.ToArray());
            }
        }
    }
}
