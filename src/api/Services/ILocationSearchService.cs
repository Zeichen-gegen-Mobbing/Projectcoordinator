using api.Models;

namespace api.Services
{
    public interface ILocationSearchService
    {
        /// <summary>
        /// Search for locations matching the given query string.
        /// </summary>
        Task<IEnumerable<LocationSearchResult>> SearchAsync(string query);
    }
}
