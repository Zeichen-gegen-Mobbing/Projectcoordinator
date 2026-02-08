using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace api;

public sealed class GetDefaultSettings(
    IUserSettingsService userSettingsService,
    ILogger<GetDefaultSettings> logger)
{
    [Authorize(Roles = "projectcoordinator")]
    [Function(nameof(GetDefaultSettings))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "default-settings")] HttpRequest req)
    {
        logger.LogInformation("Getting default cost settings");

        var settings = await userSettingsService.GetDefaultSettingsAsync();

        return new OkObjectResult(settings);
    }
}
