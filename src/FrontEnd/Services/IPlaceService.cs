using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public interface IPlaceService
    {
        Task<IEnumerable<Place>> GetAllPlacesAsync();
        Task<Place> CreatePlaceAsync(string name, double latitude, double longitude);
        Task<Place> UpdatePlaceAsync(PlaceId placeId, string name, double latitude, double longitude);
        Task DeletePlaceAsync(PlaceId placeId);
    }
}
