using System.Net.Http.Json;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class PlaceService(HttpClient httpClient) : IPlaceService
    {
        public async Task<IEnumerable<Place>> GetAllPlacesAsync()
        {
            var result = await httpClient.GetFromJsonAsync<IEnumerable<ZgM.ProjectCoordinator.Shared.Place>>("api/places");
            if (result == null)
            {
                throw new Exception("Failed to load places");
            }
            else
            {
                return result;
            }
        }

        public Task<TimeSpan> GetTripTimeAsync(PlaceId place, string address)
        {
            throw new NotImplementedException();
        }
    }
}
