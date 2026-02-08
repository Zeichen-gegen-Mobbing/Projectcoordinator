using ZgM.ProjectCoordinator.Shared;

namespace api.Models;

/// <summary>
/// User settings request with user identification - used internally by API
/// </summary>
public sealed record UserSettings
{
    public required UserId UserId { get; init; }
    public uint? CentsPerKilometer { get; init; }
    public uint? CentsPerHour { get; init; }

    /// <summary>
    /// Creates an API UserSettings from a Shared UserSettings and UserId
    /// </summary>
    public static UserSettings FromShared(ZgM.ProjectCoordinator.Shared.UserSettings request, UserId userId)
    {
        return new UserSettings
        {
            UserId = userId,
            CentsPerKilometer = request.CentsPerKilometer,
            CentsPerHour = request.CentsPerHour
        };
    }
}
