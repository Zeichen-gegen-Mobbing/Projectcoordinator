using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public interface ILocationService
    {
        /// <summary>
        /// Search for locations matching the given query string.
        /// </summary>
        Task<IEnumerable<LocationSearchResult>> SearchAsync(string query);
        
        /// <summary>
        /// Validate if the given coordinates can be used.
        /// </summary>
        Task<bool> ValidateAsync(double latitude, double longitude);
    }
}
