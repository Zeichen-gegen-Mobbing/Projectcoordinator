using ZgM.ProjectCoordinator.Shared;

namespace api.Models
{
    /// <summary>
    /// Result of car route calculation including time, distance, and cost
    /// </summary>
    public sealed class CarRouteResult
    {
        public required PlaceId PlaceId { get; init; }

        /// <summary>
        /// Travel time in seconds
        /// </summary>
        public required double DurationSeconds { get; init; }

        /// <summary>
        /// Distance in meters
        /// </summary>
        public required double DistanceMeters { get; init; }

        /// <summary>
        /// Cost in cents
        /// </summary>
        public required ushort CostCents { get; init; }
    }
}
