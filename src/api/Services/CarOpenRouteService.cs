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
    public sealed class CarOpenRouteService(
        IHttpClientFactory clientFactory,
        IOptions<OpenRouteServiceOptions> options,
        ILogger<CarOpenRouteService> logger) : ICarRouteService
    {
        private readonly HttpClient client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
        private static readonly string[] _metrics = ["duration", "distance"];
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public async IAsyncEnumerable<CarRouteResult> CalculateRoutesAsync(
            IList<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            if (places.Count == 0)
            {
                yield break;
            }

            var response = await client.PostAsJsonAsync("v2/matrix/driving-car", new
            {
                locations = places.Select(place => new[] { place.Longitude, place.Latitude })
                    .Append([originLongitude, originLatitude]),
                destinations = Enumerable.Range(places.Count, 1),
                sources = Enumerable.Range(0, places.Count),
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
            OpenRouteServiceMatrixResponse result;
            try
            {
                result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(
                    responseBody, _serializeOptions);
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

            for (int i = 0; i < places.Count; i++)
            {
                var durationSeconds = result.Durations[i].Single();
                var distanceMeters = result.Distances[i].Single();
                yield return new CarRouteResult
                {
                    Place = places[i],
                    DurationSeconds = (uint)Math.Ceiling(durationSeconds),
                    DistanceMeters = (uint)Math.Ceiling(distanceMeters),
                };
            }
        }
    }
}
