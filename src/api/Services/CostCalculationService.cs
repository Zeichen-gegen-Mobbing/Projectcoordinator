using api.Models;
using api.Repositories;
using Microsoft.Extensions.Logging;

namespace api.Services;

public sealed class CostCalculationService(
    IUserSettingRepository userSettingRepository,
    ILogger<CostCalculationService> logger) : ICostCalculationService
{
    private readonly IUserSettingRepository userSettingRepository = userSettingRepository;
    private readonly ILogger<CostCalculationService> logger = logger;
    private readonly Dictionary<ZgM.ProjectCoordinator.Shared.UserId, UserSettings?> _cache = [];

    public async Task<uint> CalculateCostAsync(ZgM.ProjectCoordinator.Shared.UserId userId, uint distanceMeters, uint durationSeconds)
    {
        var settings = await GetUserSettingAsync(userId);

        if (settings?.CentsPerHour is not null)
        {
            var hours = durationSeconds / 3600.0;
            var cost = (uint)Math.Ceiling(hours * settings.CentsPerHour.Value);
            logger.LogDebug("Calculated hour-based cost for user {UserId}: {Cost} cents ({Hours:F2} hours @ {Rate} cents/hour)",
                userId, cost, hours, settings.CentsPerHour.Value);
            return cost;
        }

        var centsPerKm = settings?.CentsPerKilometer ?? 25;
        var kilometers = distanceMeters / 1000.0;
        var kmCost = (uint)Math.Ceiling(kilometers * centsPerKm);

        logger.LogDebug("Calculated distance-based cost for user {UserId}: {Cost} cents ({Km:F2} km @ {Rate} cents/km)",
            userId, kmCost, kilometers, centsPerKm);

        return kmCost;
    }

    private async Task<UserSettings?> GetUserSettingAsync(ZgM.ProjectCoordinator.Shared.UserId userId)
    {
        if (_cache.TryGetValue(userId, out var cachedSettings))
        {
            return cachedSettings;
        }

        var settings = await userSettingRepository.GetByUserIdAsync(userId);
        _cache[userId] = settings;
        return settings;
    }
}
