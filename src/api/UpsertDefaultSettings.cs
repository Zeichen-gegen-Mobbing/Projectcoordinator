using api.Extensions;
using api.Options;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZgM.ProjectCoordinator.Shared;

namespace api;

public sealed class UpsertDefaultSettings(
    IUserSettingsService userSettingsService,
    IOptions<RoleOptions> roleOptions,
    ILogger<UpsertDefaultSettings> logger)
{
    [Authorize(Roles = "projectcoordinator")]
    [Function(nameof(UpsertDefaultSettings))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "default-settings")] HttpRequest req)
    {
        await req.HttpContext.AuthorizeAzureFunctionAsync(scopes: ["Settings.Write"], roles: [roleOptions.Value.ProjectCoordination]);
        logger.LogInformation("Upserting default cost settings");

        var settings = await req.ReadFromJsonAsync<UserSettings>();
        if (settings == null)
        {
            return new BadRequestObjectResult("Request body is required");
        }

        settings = await userSettingsService.UpsertDefaultSettingsAsync(settings);

        return new OkObjectResult(settings);
    }
}
