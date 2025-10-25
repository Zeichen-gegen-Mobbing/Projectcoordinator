using System.ComponentModel.DataAnnotations;

namespace api.Options;

public record CosmosOptions
{
    public required string Title { get; init; } = "Cosmos";

    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required string DatabaseId { get; init; }

    [Required]
    public required string ContainerId { get; init; }
}
