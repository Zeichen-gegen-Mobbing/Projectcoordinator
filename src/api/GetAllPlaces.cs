using System.Text.Json;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
                logger.LogTrace($"{nameof(GetAllPlaces)} invoked");
                var places = await placeService.GetAllPlacesAsync();
                logger.LogDebug("Returning places: {places}", places);
                return new OkObjectResult(places.ToArray());
            }
        }
    }
}
