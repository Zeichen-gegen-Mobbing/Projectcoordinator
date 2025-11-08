using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// Calculates train routes using Transitous.
    /// </summary>
    public sealed class TrainTransitousService : ITrainRouteService
    {
        private readonly HttpClient client;
        private readonly ICarRouteService carRouteService;
        private readonly ILogger<TrainTransitousService> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public TrainTransitousService(
            IHttpClientFactory clientFactory,
            ICarRouteService carRouteService,
            IOptions<TransitousOptions> options,
            ILogger<TrainTransitousService> logger)
        {
            client = clientFactory.CreateClient();
            client.BaseAddress = new Uri(options.Value.BaseUrl);

            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            client.DefaultRequestHeaders.Add("User-Agent", $"Projectcoordinator/{version} (https://z-g-m.de)");

            this.carRouteService = carRouteService;
            this.logger = logger;
        }

        public async Task<IEnumerable<TrainRouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            if (!places.Any())
            {
                return [];
            }

            var carRoutes = await carRouteService.CalculateRoutesAsync(places, originLatitude, originLongitude);
            var carCosts = carRoutes.ToDictionary(r => r.PlaceId, r => r.CostCents);

            var departureTime = GetNextWeekdayAtNoon();
            var results = new List<TrainRouteResult>();

            foreach (var place in places)
            {
                try
                {
                    var outbound = await CalculateSingleRouteAsync(
                        originLatitude, originLongitude,
                        place.Latitude, place.Longitude,
                        departureTime);

                    var returnTrip = await CalculateSingleRouteAsync(
                        place.Latitude, place.Longitude,
                        originLatitude, originLongitude,
                        departureTime);

                    var averageDuration = (outbound + returnTrip) / 2.0;
                    var costCents = carCosts.TryGetValue(place.Id, out var cost) ? cost : (ushort)0;

                    results.Add(new TrainRouteResult
                    {
                        PlaceId = place.Id,
                        DurationSeconds = averageDuration,
                        CostCents = costCents
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to calculate train route for place {PlaceId} ({PlaceName})",
                        place.Id, place.Name);
                    throw;
                }
            }

            return results;
        }

        private async Task<double> CalculateSingleRouteAsync(
            double fromLat, double fromLon,
            double toLat, double toLon,
            DateTimeOffset time)
        {
            var fromPlace = $"{fromLat.ToString(CultureInfo.InvariantCulture)},{fromLon.ToString(CultureInfo.InvariantCulture)}";
            var toPlace = $"{toLat.ToString(CultureInfo.InvariantCulture)},{toLon.ToString(CultureInfo.InvariantCulture)}";
            var timeParam = time.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

            var url = $"/api/v5/plan?fromPlace={fromPlace}&toPlace={toPlace}&time={Uri.EscapeDataString(timeParam)}&detailedTransfers=false";

            var response = await client.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError("Transitous returned error {Status}: {Content}",
                    response.StatusCode, responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Train Route Calculation Failed",
                    "Failed to calculate train routes from Transitous");
            }

            try
            {
                var result = JsonSerializer.Deserialize<TransitousPlanResponse>(
                    responseBody, _serializeOptions);

                if (result?.Itineraries == null || !result.Itineraries.Any())
                {
                    logger.LogWarning("No train routes found from {FromLat},{FromLon} to {ToLat},{ToLon}",
                        fromLat, fromLon, toLat, toLon);
                    return 0;
                }

                return result.Itineraries.First().Duration;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize Transitous response: {Content}",
                    responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Train Route Calculation Failed",
                    "Failed to parse response from Transitous");
            }
        }

        private static DateTimeOffset GetNextWeekdayAtNoon()
        {
            var now = DateTimeOffset.UtcNow;
            var candidate = new DateTimeOffset(now.Year, now.Month, now.Day, 12, 0, 0, TimeSpan.Zero);

            if (candidate <= now)
            {
                candidate = candidate.AddDays(1);
            }

            while (candidate.DayOfWeek == DayOfWeek.Saturday || candidate.DayOfWeek == DayOfWeek.Sunday)
            {
                candidate = candidate.AddDays(1);
            }

            return candidate;
        }

        private sealed class TransitousPlanResponse
        {
            public List<TransitousItinerary>? Itineraries { get; set; }
        }

        private sealed class TransitousItinerary
        {
            public double Duration { get; set; }
        }
    }
}
