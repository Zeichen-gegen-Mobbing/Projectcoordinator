using System.ComponentModel.DataAnnotations;

namespace ZgM.ProjectCoordinator.Shared;

public record AuthenticationOptions
{
    [Required]
    public required string FrontEndClientId { get; init; }

    [Required]
    public required string ApiClientId { get; init; }
}
