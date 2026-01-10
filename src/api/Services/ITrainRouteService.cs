using api.Entities;
using api.Models;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Calculates train routes including time, using car costs for pricing.
    /// Easily replaceable with different transit providers (Google Transit, Deutsche Bahn, etc.)
    /// </summary>
    public interface ITrainRouteService
    {
        /// <summary>
        /// Calculate train routes from origin to multiple places.
        /// Uses car route service internally for pricing since train pricing is not yet available.
        /// </summary>
        /// <param name="places">Places to calculate routes to</param>
        /// <param name="originLatitude">Starting point latitude</param>
        /// <param name="originLongitude">Starting point longitude</param>
        /// <returns>Route results including train time and car-based cost</returns>
        IAsyncEnumerable<TrainRouteResult> CalculateRoutesAsync(
            IList<PlaceEntity> places,
            double originLatitude,
            double originLongitude);
    }
}
