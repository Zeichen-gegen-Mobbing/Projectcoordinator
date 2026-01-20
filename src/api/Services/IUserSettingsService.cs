using ZgM.ProjectCoordinator.Shared;

namespace api.Services;

public interface IUserSettingsService
{
    Task<Models.UserSettings?> GetUserSettingsAsync(UserId userId);
    Task<Models.UserSettings> UpsertUserSettingsAsync(Models.UserSettings settings);
    Task DeleteUserSettingsAsync(UserId userId);
    Task<UserSettings> GetDefaultSettingsAsync();
    Task<UserSettings> UpsertDefaultSettingsAsync(UserSettings settings);
}
