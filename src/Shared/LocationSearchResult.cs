namespace ZgM.ProjectCoordinator.Shared
{
    public struct LocationSearchResult
    {
        public required string Label { get; init; }
        public required double Longitude { get; init; }
        public required double Latitude { get; init; }
        public string? Name { get; init; }
        public string? Street { get; init; }
        public string? HouseNumber { get; init; }
        public string? PostalCode { get; init; }
        public string? Country { get; init; }
        public string? Region { get; init; }
        public string? County { get; init; }
        public string? Locality { get; init; }
    }
}
