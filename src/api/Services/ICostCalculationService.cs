using api.Entities;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services;

public interface ICostCalculationService
{
    /// <summary>
    /// Calculates cost based on distance or time depending on user-specific settings
    /// </summary>
    /// <param name="userId">User ID to check for custom cost settings</param>
    /// <param name="distanceMeters">Distance in meters</param>
    /// <param name="durationSeconds">Duration in seconds (used for hour-based calculation)</param>
    /// <returns>Cost in cents</returns>
    Task<uint> CalculateCostAsync(UserId userId, uint distanceMeters, uint durationSeconds);
}
