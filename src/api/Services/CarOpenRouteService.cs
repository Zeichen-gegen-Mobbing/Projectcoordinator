using System.Net.Http.Json;
using System.Text.Json;
using api.Entities;
using api.Exceptions;
using api.Extensions;
using api.Models;
using api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Calculates car routes using OpenRouteService driving-car profile.
    /// </summary>
    public sealed class CarOpenRouteService : ICarRouteService
    {
        private readonly HttpClient client;
        private readonly ILogger<CarOpenRouteService> logger;
        private static readonly string[] _metrics = ["duration", "distance"];
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public CarOpenRouteService(
            IHttpClientFactory clientFactory,
            IOptions<OpenRouteServiceOptions> options,
            ILogger<CarOpenRouteService> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public async Task<IEnumerable<CarRouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            if (!places.Any())
            {
                return Enumerable.Empty<CarRouteResult>();
            }

            var response = await client.PostAsJsonAsync("v2/matrix/driving-car", new
            {
                locations = places.Select(place => new[] { place.Longitude, place.Latitude })
                    .Append([originLongitude, originLatitude])
                    .ToArray(),
                destinations = Enumerable.Range(places.Count(), 1),
                sources = Enumerable.Range(0, places.Count()),
                metrics = _metrics
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError("OpenRouteService returned error {Status}: {Content}",
                    response.StatusCode, responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Route Calculation Failed",
                    "Failed to calculate car routes from OpenRouteService");
            }

            try
            {
                var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(
                    responseBody, _serializeOptions);

                return places.Select((place, index) =>
                {
                    var durationSeconds = result.Durations[index].Single();
                    var distanceMeters = result.Distances[index].Single();
                    var costCents = (ushort)(Math.Ceiling(distanceMeters / 1000) * 25);

                    return new CarRouteResult
                    {
                        PlaceId = place.Id,
                        DurationSeconds = durationSeconds,
                        DistanceMeters = distanceMeters,
                        CostCents = costCents
                    };
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize OpenRouteService response: {Content}",
                    responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Route Calculation Failed",
                    "Failed to parse response from OpenRouteService");
            }
        }
    }
}
