using api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace ZgM.Projectcoordinator.api;

public class GetAuthenticationConfiguration(ILogger<GetAuthenticationConfiguration> logger, IOptions<AuthenticationOptions> authOptions)
{
    [Function(nameof(GetAuthenticationConfiguration))]
    [ProducesResponseType(200, Type = typeof(ProjectCoordinator.Shared.AuthenticationOptions))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authentication-config")] HttpRequest req)
    {
        using (logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(GetAuthenticationConfiguration) } }))
        {
            logger.LogTrace($"{nameof(GetAuthenticationConfiguration)} invoked");
            return new OkObjectResult(authOptions.Value.ToSharedOptions());
        }
    }
}
