using api.Models;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public interface IPlaceService
    {
        Task<IEnumerable<Place>> GetAllPlacesAsync();
        Task<IEnumerable<Place>> GetPlacesAsync(UserId userId);
        Task<Place> AddPlace(Models.PlaceRequest placeRequest);
        Task DeletePlace(UserId userId, PlaceId placeId);
    }
}
