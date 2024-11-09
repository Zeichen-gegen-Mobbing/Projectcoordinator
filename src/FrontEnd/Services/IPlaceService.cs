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
        /// Get Trip time from given Place to address
        /// </summary>
        /// <param name="place">The id of place to load the trip time</param>
        /// <param name="address">The adress to end the trips</param>
        public Task<TimeSpan> GetTripTimeAsync(PlaceId place, string address);
    }
}
