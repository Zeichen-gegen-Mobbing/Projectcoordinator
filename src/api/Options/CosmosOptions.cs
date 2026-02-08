using System.ComponentModel.DataAnnotations;

namespace api.Options;

public record CosmosOptions
{
    public required string Title { get; init; } = "Cosmos";

    [Required]
    public required string ConnectionString { get; init; }

    [Required]
    public required string DatabaseId { get; init; } = "cosql-shared-free-zgm";

    [Required]
    public required string PlacesContainerId { get; init; } = "Projectcoordinator-Places";

    [Required]
    public required string UserSettingsContainerId { get; init; } = "Projectcoordinator-UserSettings";
}
