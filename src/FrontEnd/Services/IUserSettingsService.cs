using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public interface IUserSettingsService
    {
        Task<UserSettings?> GetUserSettingsAsync(UserId userId);
        Task<UserSettings> UpsertUserSettingsAsync(UserId userId, UserSettings settings);
        Task DeleteUserSettingsAsync(UserId userId);
    }
}
