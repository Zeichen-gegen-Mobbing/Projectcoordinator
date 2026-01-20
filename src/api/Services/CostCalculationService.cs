using Microsoft.Extensions.Logging;

namespace api.Services;

public sealed class CostCalculationService(
    IUserSettingsService userSettingsService,
    ILogger<CostCalculationService> logger) : ICostCalculationService
{
    public async Task<uint> CalculateCostAsync(ZgM.ProjectCoordinator.Shared.UserId userId, uint distanceMeters, uint durationSeconds)
    {
        var settings = await userSettingsService.GetUserSettingsAsync(userId);

        if (settings?.CentsPerHour is not null)
        {
            var hours = durationSeconds / 3600.0;
            var cost = (uint)Math.Ceiling(hours * settings.CentsPerHour.Value);
            logger.LogDebug("Calculated hour-based cost for user {UserId}: {Cost} cents ({Hours:F2} hours @ {Rate} cents/hour)",
                userId, cost, hours, settings.CentsPerHour.Value);
            return cost;
        }

        uint centsPerKm;
        if (settings?.CentsPerKilometer is not null)
        {
            centsPerKm = settings.CentsPerKilometer.Value;
        }
        else
        {
            var defaultSettings = await userSettingsService.GetDefaultSettingsAsync();
            centsPerKm = defaultSettings.CentsPerKilometer ?? 25;
        }

        var kilometers = distanceMeters / 1000.0;
        var kmCost = (uint)Math.Ceiling(kilometers * centsPerKm);

        logger.LogDebug("Calculated distance-based cost for user {UserId}: {Cost} cents ({Km:F2} km @ {Rate} cents/km)",
            userId, kmCost, kilometers, centsPerKm);

        return kmCost;
    }
}

