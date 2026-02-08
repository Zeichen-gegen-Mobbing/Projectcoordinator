using System.ComponentModel.DataAnnotations;

namespace api.Options;

public record RoleOptions
{
    public required string Title { get; init; } = "Roles";

    [Required]
    public required string ProjectCoordination { get; init; } = "projectcoordination";

    [Required]
    public required string Admin { get; init; } = "admin";
}
