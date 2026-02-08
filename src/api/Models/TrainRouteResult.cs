using api.Entities;

namespace api.Models
{
    /// <summary>
    /// Result of train route calculation including time and cost (from car)
    /// </summary>
    public sealed class TrainRouteResult
    {
        public required PlaceEntity Place { get; init; }

        /// <summary>
        /// Travel time in seconds (via train)
        /// </summary>
        public required uint DurationSeconds { get; init; }
    }
}
