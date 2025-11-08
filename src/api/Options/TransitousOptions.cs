using System.ComponentModel.DataAnnotations;

namespace api.Options;

public record TransitousOptions
{
    public required string Title { get; init; } = "Transitous";

    [Required]
    public required string BaseUrl { get; init; } = "https://api.transitous.org";
}
