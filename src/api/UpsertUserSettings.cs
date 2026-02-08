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

/// <summary>
/// Azure Function to create or update user cost settings
/// </summary>
public sealed class UpsertUserSettings(
    IUserSettingsService userSettingsService,
    ILogger<UpsertUserSettings> logger,
    IOptions<RoleOptions> roleOptions)
{
    [Function("UpsertUserSettings")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "users/{userId}/settings")] HttpRequest req,
        string userId)
    {
        using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(UpsertUserSettings) } }))
        {
            logger.LogInformation("Upserting cost settings for user {UserId}", userId);
            await req.HttpContext.AuthorizeAzureFunctionAsync(scopes: ["Settings.Write"], roles: [roleOptions.Value.ProjectCoordination]);

            var requestedUserId = UserId.Parse(userId);
            var sharedRequest = await req.ReadFromJsonAsync<ZgM.ProjectCoordinator.Shared.UserSettings>();
            if (sharedRequest == null)
            {
                return new BadRequestObjectResult("Request body is required");
            }

            var settings = await userSettingsService.UpsertUserSettingsAsync(new Models.UserSettings
            {
                UserId = requestedUserId,
                CentsPerKilometer = sharedRequest.CentsPerKilometer,
                CentsPerHour = sharedRequest.CentsPerHour
            });

            return new OkObjectResult(settings);
        }
    }
}
