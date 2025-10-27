using api.Models;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public interface IPlaceService
    {
        Task<IEnumerable<Place>> GetAllPlacesAsync();
        Task<IEnumerable<Place>> GetPlacesByUserIdAsync(UserId userId);
        Task<Place> AddPlace(PlaceRequest placeRequest);
        Task<Place> UpdatePlace(PlaceId placeId, PlaceRequest placeRequest);
        Task DeletePlace(PlaceId placeId, UserId userId);
    }
}
