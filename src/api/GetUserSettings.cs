using System.Net;
using api.Extensions;
using api.Options;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZgM.ProjectCoordinator.Shared;

namespace api;

public sealed class GetUserSettings(IUserSettingsService userSettingsService, ILogger<GetUserSettings> logger, IOptions<RoleOptions> roleOptions)
{
    [Function("GetUserSettings")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{userId}/settings")] HttpRequest req,
        string userId)
    {
        using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetUserSettings) } }))
        {
            logger.LogInformation("Getting cost settings for user {UserId}", userId);
            logger.LogDebug("Only Role {Role} is allowed", roleOptions.Value.ProjectCoordination);
            await req.HttpContext.AuthorizeAzureFunctionAsync(
                scopes: ["Settings.Read"],
                roles: [roleOptions.Value.ProjectCoordination]);

            var userIdValue = UserId.Parse(userId);
            var settings = await userSettingsService.GetUserSettingsAsync(userIdValue);

            if (settings == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(settings);
        }
    }
}
