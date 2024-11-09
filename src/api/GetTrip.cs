using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;

namespace ZgM.Projectcoordinator.api
{
    public class GetTrip
    {
        private readonly ILogger<GetTrip> _logger;

        public GetTrip(ILogger<GetTrip> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get information about the trip from given place to given address. The address should be encoded according to RFC 2396 (eg. using Uri.EscapeDataString)
        /// </summary>
        [Function(nameof(GetTrip))]
        [ProducesResponseType(200, Type = typeof(Trip))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "places/{placeId}/trips/{address}")] HttpRequest req, string placeId, string address)
        {
            Random random = new Random();
            await Task.Delay(random.Next(random.Next(1000)));

            Trip trip = new Trip() 
            { 
                PlaceId = new PlaceId(placeId),
                Time = new TimeSpan(random.Next(8), random.Next(59), random.Next(59)),
                Cost = (ushort)random.Next(ushort.MaxValue)
            };

            return new OkObjectResult(trip);
        }
   }
}
