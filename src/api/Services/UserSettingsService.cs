using System.Net;
using api.Exceptions;
using api.Repositories;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services;

public sealed class UserSettingsService(
    IUserSettingRepository userSettingRepository,
    ILogger<UserSettingsService> logger) : IUserSettingsService
{
    private static readonly UserId DefaultSettingsUserId = UserId.Parse("00000000-0000-0000-0000-000000000000");

    public async Task<Models.UserSettings?> GetUserSettingsAsync(UserId userId)
    {
        ValidateNotDefaultUserId(userId);

        logger.LogDebug("Getting user settings for {UserId}", userId);
        return await userSettingRepository.GetByUserIdAsync(userId);
    }

    public async Task<Models.UserSettings> UpsertUserSettingsAsync(Models.UserSettings settings)
    {
        ValidateNotDefaultUserId(settings.UserId);

        logger.LogDebug("Upserting user settings for {UserId}", settings.UserId);

        await userSettingRepository.UpsertAsync(settings);

        logger.LogInformation(
            "Successfully upserted settings for user {UserId}: CentsPerKm={CentsPerKm}, CentsPerHour={CentsPerHour}",
            settings.UserId,
            settings.CentsPerKilometer,
            settings.CentsPerHour);

        return settings;
    }

    public async Task DeleteUserSettingsAsync(UserId userId)
    {
        ValidateNotDefaultUserId(userId);

        logger.LogDebug("Deleting user settings for {UserId}", userId);
        await userSettingRepository.DeleteAsync(userId);

        logger.LogInformation("Successfully deleted settings for user {UserId}", userId);
    }

    public async Task<UserSettings> GetDefaultSettingsAsync()
    {
        logger.LogDebug("Getting default cost settings");

        var settings = await userSettingRepository.GetByUserIdAsync(DefaultSettingsUserId);

        return new UserSettings
        {
            CentsPerKilometer = settings?.CentsPerKilometer ?? 25
        };

    }

    public async Task<UserSettings> UpsertDefaultSettingsAsync(UserSettings settings)
    {
        logger.LogDebug("Upserting default cost settings");

        if (settings.CentsPerHour is not null)
        {
            throw new ProblemDetailsException(HttpStatusCode.BadRequest, "CentsPerHour not supported", "Only CentsPerKilometer supported as default Settings.");
        }

        var defaultSettings = new Models.UserSettings
        {
            UserId = DefaultSettingsUserId,
            CentsPerKilometer = settings.CentsPerKilometer,
        };

        await userSettingRepository.UpsertAsync(defaultSettings);

        logger.LogInformation(
            "Successfully upserted default settings: CentsPerKm={CentsPerKm}",
            defaultSettings.CentsPerKilometer);

        return settings;
    }

    private static void ValidateNotDefaultUserId(UserId userId)
    {
        if (userId.Equals(DefaultSettingsUserId))
        {
            throw new ProblemDetailsException(
                System.Net.HttpStatusCode.BadRequest,
                "Invalid user ID",
                "Cannot perform this operation on the default settings user ID");
        }
    }
}
