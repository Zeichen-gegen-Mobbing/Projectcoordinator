using api.Entities;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public interface IPlaceService
    {
        Task<IEnumerable<Place>> GetAllPlacesAsync();
        Task AddPlace(PlaceEntity place);
    }
}
