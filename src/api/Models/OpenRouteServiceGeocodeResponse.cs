using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    public struct OpenRouteServiceGeocodeResponse
    {
        [Required]
        public required Feature[] Features { get; init; }

        public struct Feature
        {
            [Required]
            public required Geometry Geometry { get; init; }
            [Required]
            public required Properties Properties { get; init; }
        }

        public struct Geometry
        {
            [Required]
            public required double[] Coordinates { get; init; }
        }

        public struct Properties
        {
            [Required]
            public required string Label { get; init; }
            public string? Name { get; init; }
            public string? Street { get; init; }
            public string? Housenumber { get; init; }
            public string? Postalcode { get; init; }
            public string? Country { get; init; }
            public string? Region { get; init; }
            public string? County { get; init; }
            public string? Locality { get; init; }
        }
    }
}
