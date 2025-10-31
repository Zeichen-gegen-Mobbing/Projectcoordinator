using ZgM.ProjectCoordinator.Shared;

namespace api.Models
{
    /// <summary>
    /// Place request with user identification - used internally by API
    /// </summary>
    public struct PlaceRequest
    {
        public required UserId UserId { get; init; }
        public required string Name { get; init; }
        public required double Longitude { get; init; }
        public required double Latitude { get; init; }

        /// <summary>
        /// Creates an API PlaceRequest from a Shared PlaceRequest and UserId
        /// </summary>
        public static PlaceRequest FromShared(ZgM.ProjectCoordinator.Shared.PlaceRequest request, UserId userId)
        {
            return new PlaceRequest
            {
                UserId = userId,
                Name = request.Name,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };
        }
    }
}
