using api.Models;

namespace api.Repositories;

public interface IUserSettingRepository
{
    /// <summary>
    /// Gets custom cost settings for a specific user
    /// </summary>
    Task<UserSettings?> GetByUserIdAsync(ZgM.ProjectCoordinator.Shared.UserId userId);

    /// <summary>
    /// Creates or updates cost settings for a user
    /// </summary>
    Task UpsertAsync(UserSettings settings);

    /// <summary>
    /// Deletes cost settings for a user, reverting to defaults
    /// </summary>
    Task DeleteAsync(ZgM.ProjectCoordinator.Shared.UserId userId);
}
