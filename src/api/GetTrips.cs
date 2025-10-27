using System.Globalization;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using ZgM.ProjectCoordinator.Shared;

namespace ZgM.Projectcoordinator.api
{
    public class GetTrips
    {
        private readonly ILogger<GetTrips> _logger;
        private readonly ITripService _tripService;

        public GetTrips(ILogger<GetTrips> logger, ITripService tripService)
        {
            _logger = logger;
            _tripService = tripService;
        }

        /// <summary>
        /// Get Trips from all saved places to latitude and longitude from query parameter. The Latitude and Longitude should use dot as decimal seperator and WGS 84 (EPSG:4326)
        /// </summary>
        [Function(nameof(GetTrips))]
        [ProducesResponseType(200, Type = typeof(IEnumerable<Trip>))]
        [RequiredScope("Trips.GetAll")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "trips")] HttpRequest req)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetTrips) } }))
            {
                var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
                if (!authenticationStatus)
                {
                    _logger.LogWarning("Unauthenticated request: Authorization Header: {AuthHeader}", req.Headers.Authorization.ToString());
                    return authenticationResponse!;
                }


                if (!double.TryParse(req.Query["latitude"], CultureInfo.GetCultureInfo("en-US"), out double latitude) || !double.TryParse(req.Query["longitude"], CultureInfo.GetCultureInfo("en-US"), out double longitude))
                {
                    return new BadRequestObjectResult(new ProblemDetails()
                    {
                        Title = "Invalid Trip Destination",
                        Detail = $"Invalid latitude ({req.Query["latitude"]}) or longitude ({req.Query["longitude"]})",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                _logger.LogDebug("Returning trips from places to the specified latitude and longitude.");
                var trips = await _tripService.GetAllTripsAsync(latitude, longitude);
                return new OkObjectResult(trips);
            }
        }
    }
}
