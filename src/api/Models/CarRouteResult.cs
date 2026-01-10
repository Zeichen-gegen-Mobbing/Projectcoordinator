using api.Entities;

namespace api.Models
{
    /// <summary>
    /// Result of car route calculation including time, distance, and cost
    /// </summary>
    public sealed class CarRouteResult
    {
        public required PlaceEntity Place { get; init; }

        /// <summary>
        /// Travel time in seconds
        /// </summary>
        public required uint DurationSeconds { get; init; }

        /// <summary>
        /// Distance in meters
        /// </summary>
        public required uint DistanceMeters { get; init; }
    }
}
