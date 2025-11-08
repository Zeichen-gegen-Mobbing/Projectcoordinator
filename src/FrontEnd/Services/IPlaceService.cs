using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public interface IPlaceService
    {
        Task<IEnumerable<Place>> GetPlacesAsync(UserId userId);
        Task DeletePlaceAsync(UserId userId, PlaceId placeId);
        Task<Place> CreatePlaceAsync(UserId userId, string name, double latitude, double longitude, TransportMode transportMode);
    }
}
