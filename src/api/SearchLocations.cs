using api.Exceptions;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace api
{
    public class SearchLocations
    {
        private readonly ILogger<SearchLocations> _logger;
        private readonly ILocationService _locationService;

        public SearchLocations(ILogger<SearchLocations> logger, ILocationService locationService)
        {
            _logger = logger;
            _locationService = locationService;
        }

        [Function(nameof(SearchLocations))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "locations")] HttpRequest request)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(SearchLocations) } }))
            {
                var (authenticationStatus, authenticationResponse) = await request.HttpContext.AuthenticateAzureFunctionAsync();
                if (!authenticationStatus)
                    return authenticationResponse!;

                var text = request.Query["text"].ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new BadRequestObjectResult(new { error = "Query parameter 'text' is required" });
                }

                _logger.LogInformation("Searching for locations with query: {Query}", text);
                try
                {
                    var results = await _locationService.SearchAsync(text);
                    return new OkObjectResult(results);
                }
                catch (ProblemDetailsException ex)
                {
                    return new ObjectResult(ex.ProblemDetails) { StatusCode = ex.ProblemDetails.Status };
                }
            }
        }
    }
}
