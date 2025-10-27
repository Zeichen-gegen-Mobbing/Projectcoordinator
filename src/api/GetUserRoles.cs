using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace ZgM.Projectcoordinator.api;

public class GetUserRoles(ILogger<GetUserRoles> logger)
{
    [Function(nameof(GetUserRoles))]
    [ProducesResponseType(200, Type = typeof(string[]))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "user/roles")] HttpRequest req)
    {
        using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetUserRoles) } }))
        {
            var (authenticationStatus, authenticationResponse) = await req.HttpContext.AuthenticateAzureFunctionAsync();
            if (!authenticationStatus)
            {
                logger.LogWarning("Unauthenticated request: {response}", authenticationResponse!.ToString());
                return authenticationResponse!;
            }

            var roles = req.HttpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                .Select(c => c.Value)
                .ToArray();

            logger.LogDebug("Returning {RoleCount} roles for user", roles.Length);
            return new OkObjectResult(roles);
        }
    }
}
