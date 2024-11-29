using api.Entities;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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

        [Function("CreatePlace")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Admin, "post")] HttpRequest request)
        {
            var place = await request.ReadFromJsonAsync<PlaceEntity>();
            await _placeService.AddPlace(place);
            return new CreatedResult();
        }
    }
}
