using System.Net;
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

public sealed class GetUserSettings(IUserSettingRepository repository, ILogger<GetUserSettings> logger)
{
    [Function("GetUserSettings")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{userId}/settings")] HttpRequest req,
        string userId, IOptions<RoleOptions> roleOptions)
    {
        using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetUserSettings) } }))
        {
            logger.LogInformation("Getting cost settings for user {UserId}", userId);
            await req.HttpContext.AuthorizeAzureFunctionAsync(scopes: ["Settings.Read"], roles: [roleOptions.Value.ProjectCoordination]);

            var userIdValue = UserId.Parse(userId);
            var settings = await repository.GetByUserIdAsync(userIdValue);

            if (settings == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(settings);
        }
    }
}
