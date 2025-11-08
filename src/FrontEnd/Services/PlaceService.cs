using ZgM.ProjectCoordinator.Shared;
using System.Net.Http.Json;

namespace FrontEnd.Services
{
    public class PlaceService(HttpClient httpClient) : IPlaceService
    {
        public async Task<IEnumerable<Place>> GetPlacesAsync(UserId userId)
        {
            var response = await httpClient.GetAsync($"/api/users/{userId.Value}/places");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<Place>>() ?? [];
        }

        public async Task DeletePlaceAsync(UserId userId, PlaceId placeId)
        {
            var response = await httpClient.DeleteAsync($"/api/users/{userId.Value}/places/{placeId.Value}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<Place> CreatePlaceAsync(UserId userId, string name, double latitude, double longitude, TransportMode transportMode)
        {
            var placeRequest = new PlaceRequest
            {
                Name = name,
                Latitude = latitude,
                Longitude = longitude,
                TransportMode = transportMode
            };

            var response = await httpClient.PostAsJsonAsync($"/api/users/{userId.Value}/places", placeRequest);
            response.EnsureSuccessStatusCode();
            var place = await response.Content.ReadFromJsonAsync<Place?>();
            if (place == null || place.Value.Id == default)
            {
                throw new InvalidOperationException("Failed to create place");
            }
            return place.Value;
        }
    }
}
