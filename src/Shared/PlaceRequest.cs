namespace ZgM.ProjectCoordinator.Shared
{
    /// <summary>
    /// Place request without user identification - used for creating places
    /// </summary>
    public struct PlaceRequest
    {
        public required string Name { get; init; }
        public required double Longitude { get; init; }
        public required double Latitude { get; init; }
    }
}
