using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationSearchResult>> SearchLocationsAsync(string query);
    }
}
