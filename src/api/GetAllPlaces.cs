using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;

namespace ZgM.Projectcoordinator.api
{
    public class GetAllPlaces
    {
        private readonly ILogger<GetAllPlaces> _logger;

        public static readonly IEnumerable<Place> _places = new List<Place>
        {
            new Place { Id = new PlaceId("P1"), UserId= new UserId(Guid.NewGuid()), Name = "Fake BE Place 1" },
            new Place { Id = new PlaceId("P2"),UserId= new UserId(Guid.NewGuid()), Name = "Fake BE Place 2" },
            new Place { Id = new PlaceId("P3"),UserId= new UserId(Guid.NewGuid()), Name = "Fake BE Place 3" },
            new Place { Id = new PlaceId("P4"),UserId= new UserId(Guid.NewGuid()), Name = "Fake BE Place 4" },
            new Place { Id = new PlaceId("P5"),UserId= new UserId(Guid.NewGuid()), Name = "Fake BE Place 5" },
        };

        public GetAllPlaces(ILogger<GetAllPlaces> logger)
        {
            _logger = logger;
        }

        [Function(nameof(GetAllPlaces))]
        [ProducesResponseType(200, Type = typeof(Place[]))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "places")] HttpRequest req)
        {
            _logger.LogInformation(Environment.GetEnvironmentVariable("AzureWebJobsFeatureFlags"));
            _logger.LogInformation(JsonSerializer.Serialize(_places));
            Random random = new Random();
            await Task.Delay(random.Next(random.Next(10000)));
            return new OkObjectResult(_places.ToArray());
        }
    }
}
