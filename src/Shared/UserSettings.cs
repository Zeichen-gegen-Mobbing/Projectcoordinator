namespace ZgM.ProjectCoordinator.Shared;

/// <summary>
/// The user-specific settings for cost calculations
/// </summary>
public sealed record UserSettings
{
    /// <summary>
    /// Custom cost per kilometer in cents. If null, uses default..
    /// </summary>
    public uint? CentsPerKilometer { get; init; }

    /// <summary>
    /// Custom cost per hour in cents. If set, uses time-based calculation instead of distance-based.
    /// Takes precedence over CentsPerKilometer.
    /// </summary>
    public uint? CentsPerHour { get; init; }
}
