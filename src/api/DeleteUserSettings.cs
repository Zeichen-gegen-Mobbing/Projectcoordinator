using System.Net;
using api.Extensions;
using api.Options;
using api.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZgM.ProjectCoordinator.Shared;

namespace api;

public sealed class DeleteUserSettings(IUserSettingRepository repository, ILogger<DeleteUserSettings> logger, IOptions<RoleOptions> roleOptions)
{

    [Function("DeleteUserSettings")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "users/{userId}/settings")] HttpRequest req,
        string userId)
    {
        using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(DeleteUserSettings) } }))
        {
            logger.LogInformation("Deleting cost settings for user {UserId}", userId);
            await req.HttpContext.AuthorizeAzureFunctionAsync(scopes: ["Settings.Delete"], roles: [roleOptions.Value.ProjectCoordination]);

            var userIdValue = UserId.Parse(userId);
            await repository.DeleteAsync(userIdValue);

            return new NoContentResult();
        }
    }
}
