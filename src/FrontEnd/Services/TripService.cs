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
                Console.WriteLine(httpClient.BaseAddress?.ToString() ?? "No BaseAddress set");
                var result = await httpClient.GetFromJsonAsync<IEnumerable<Trip>>(String.Format(new CultureInfo("en-US"), "api/trips?latitude={0}&longitude={1}", latitude, longitude));
                if (result == null)
                {
                    throw new Exception("Received null");
                }
                else
                {
                    return result;
                }
            }
        }
    }
}
