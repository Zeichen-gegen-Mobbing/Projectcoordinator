using api.Extensions;
using api.Options;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api;

public sealed class GetDefaultSettings(
    IUserSettingsService userSettingsService,
    IOptions<RoleOptions> roleOptions,
    ILogger<GetDefaultSettings> logger)
{
    [Authorize(Roles = "projectcoordinator")]
    [Function(nameof(GetDefaultSettings))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "default-settings")] HttpRequest req)
    {
        logger.LogInformation("Getting default cost settings");

        await req.HttpContext.AuthorizeAzureFunctionAsync(
            scopes: ["Settings.Read"],
            roles: [roleOptions.Value.ProjectCoordination]);

        var settings = await userSettingsService.GetDefaultSettingsAsync();

        return new OkObjectResult(settings);
    }
}
