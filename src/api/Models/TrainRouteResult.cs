using ZgM.ProjectCoordinator.Shared;

namespace api.Models
{
    /// <summary>
    /// Result of train route calculation including time and cost (from car)
    /// </summary>
    public sealed class TrainRouteResult
    {
        public required PlaceId PlaceId { get; init; }

        /// <summary>
        /// Travel time in seconds (via train)
        /// </summary>
        public required double DurationSeconds { get; init; }

        /// <summary>
        /// Cost in cents (from car route calculation)
        /// </summary>
        public required ushort CostCents { get; init; }
    }
}
