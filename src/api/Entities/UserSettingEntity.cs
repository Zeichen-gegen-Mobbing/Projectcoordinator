using ZgM.ProjectCoordinator.Shared;

namespace api.Entities;

/// <summary>
/// Custom cost settings for a specific user (socialvisionary)
/// </summary>
public sealed record UserSettingEntity
{
    /// <summary>
    /// Cosmos DB document id - same as UserId for this entity type
    /// </summary>
    public required UserId Id { get; init; }

    /// <summary>
    /// Custom cost per kilometer in cents. If null, uses default.
    /// </summary>
    public uint? CentsPerKilometer { get; init; }

    /// <summary>
    /// Custom cost per hour in cents. If set, uses time-based calculation instead of distance-based.
    /// </summary>
    public uint? CentsPerHour { get; init; }
}
