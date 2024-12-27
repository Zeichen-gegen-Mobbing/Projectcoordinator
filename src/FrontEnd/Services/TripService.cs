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
                var result = await httpClient.GetFromJsonAsync<IEnumerable<Trip>>($"api/trips?latitude={latitude}&longitude={longitude}");
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
