using api.Extensions;
using api.Options;
using api.Repositories;
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
    IUserSettingRepository repository,
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

            // Create UserSettings model
            var settings = new Models.UserSettings
            {
                UserId = requestedUserId,
                CentsPerKilometer = sharedRequest.CentsPerKilometer,
                CentsPerHour = sharedRequest.CentsPerHour
            };

            await repository.UpsertAsync(settings);

            logger.LogInformation(
                "Successfully upserted cost settings for user {UserId}: CentsPerKm={CentsPerKm}, CentsPerHour={CentsPerHour}",
                userId,
                settings.CentsPerKilometer,
                settings.CentsPerHour);

            return new OkObjectResult(settings);
        }
    }
}
