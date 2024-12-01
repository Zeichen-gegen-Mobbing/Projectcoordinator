using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public interface IPlaceService
    {
        /// <summary>
        /// Get all available places
        /// </summary>
        /// <returns>A Task resolving to a enumerable of places</returns>
        public Task<IEnumerable<Place>> GetAllPlacesAsync();

        /// <summary>
        /// Get Trip from given Place to address
        /// </summary>
        /// <param name="place">The id of place to load the trip</param>
        /// <param name="address">The adress to end the trip</param>
        public Task<Trip> GetTripAsync(PlaceId place, string address);
    }
}
