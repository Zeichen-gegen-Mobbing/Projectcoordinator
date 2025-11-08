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
        public required uint DurationSeconds { get; init; }

        /// <summary>
        /// Distance in meters
        /// </summary>
        public required uint DistanceMeters { get; init; }

        /// <summary>
        /// Cost in cents
        /// </summary>
        public required uint CostCents { get; init; }
    }
}
