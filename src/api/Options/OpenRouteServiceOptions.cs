using System.ComponentModel.DataAnnotations;

namespace api.Options;

public record OpenRouteServiceOptions
{
    public required string Title { get; init; } = "OpenRouteService";

    [Required]
    public required string BaseUrl { get; init; }

    [Required]
    public required string ApiKey { get; init; }
}
