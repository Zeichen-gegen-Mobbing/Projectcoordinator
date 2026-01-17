using System.Globalization;
using System.Net.Http.Json;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class TripService(HttpClient httpClient, ILogger<TripService> logger) : ITripService
    {
        public async Task<IEnumerable<Trip>> GetTripsAsync(double latitude, double longitude)
        {
            using (logger.BeginScope(nameof(GetTripsAsync)))
            {
                // Use relative URI string (not Uri object) because HttpClient has BaseAddress configured
                var requestUri = string.Format(new CultureInfo("en-US"), "trips?latitude={0}&longitude={1}", latitude, longitude);

                var result = await httpClient.GetFromJsonAsync<IEnumerable<Trip>>(requestUri);
                if (result is null)
                {
                    throw new InvalidOperationException("Received null, thats very unexpected");
                }
                else
                {
                    return result;
                }
            }
        }
    }
}
