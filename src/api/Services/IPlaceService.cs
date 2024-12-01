using api.Models;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public interface IPlaceService
    {
        Task<IEnumerable<Place>> GetAllPlacesAsync();
        Task<Place> AddPlace(PlaceRequest place);
    }
}
