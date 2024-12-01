using System.Net.Http.Json;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class PlaceService(HttpClient httpClient, ILogger<PlaceService> logger) : IPlaceService
    {
        public async Task<IEnumerable<Place>> GetAllPlacesAsync()
        {
            using (logger.BeginScope(nameof(GetAllPlacesAsync))) 
            {
                var result = await httpClient.GetFromJsonAsync<IEnumerable<ZgM.ProjectCoordinator.Shared.Place>>("api/places");
                if (result == null)
                {
                    logger.LogWarning("Received null");
                    return new List<Place>();
                }
                else
                {
                    return result;
                }
            }
        }

        public async Task<Trip> GetTripAsync(PlaceId place, string address)
        {
            return await httpClient.GetFromJsonAsync<Trip>($"api/places/{place}/trips/{Uri.EscapeDataString(address)}");
        }
    }
}
