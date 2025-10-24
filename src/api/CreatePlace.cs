using api.Entities;
using api.Exceptions;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace api
{
    public class CreatePlace
    {
        private readonly ILogger<CreatePlace> _logger;
        private readonly IPlaceService _placeService;

        public CreatePlace(ILogger<CreatePlace> logger, IPlaceService placeService)
        {
            _logger = logger;
            _placeService = placeService;
        }

        [Function(nameof(CreatePlace))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "places")] HttpRequest request)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "FunctionName", nameof(CreatePlace) } }))
            {
                var (authenticationStatus, authenticationResponse) = await request.HttpContext.AuthenticateAzureFunctionAsync();
                if (!authenticationStatus)
                    return authenticationResponse!;

                var placeRequest = await request.ReadFromJsonAsync<PlaceRequest>();
                _logger.LogInformation("Read place from request");
                try
                {
                    await _placeService.AddPlace(placeRequest);
                    return new CreatedResult();
                }
                catch (ProblemDetailsException ex)
                {
                    return new BadRequestObjectResult(ex.ProblemDetails);
                }
            }
        }
    }
}
