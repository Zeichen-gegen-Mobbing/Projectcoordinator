using api.Entities;
using api.Models;

namespace api.Services
{
    /// <summary>
    /// Calculates car routes including time, distance, and cost.
    /// Easily replaceable with different providers (HERE Maps, Google Maps, etc.)
    /// </summary>
    public interface ICarRouteService
    {
        /// <summary>
        /// Calculate car routes from origin to multiple places.
        /// Returns duration, distance, and cost for each place.
        /// </summary>
        /// <param name="places">Places to calculate routes to</param>
        /// <param name="originLatitude">Starting point latitude</param>
        /// <param name="originLongitude">Starting point longitude</param>
        /// <returns>Route results including time, distance, and cost</returns>
        Task<IEnumerable<CarRouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude);
    }
}
