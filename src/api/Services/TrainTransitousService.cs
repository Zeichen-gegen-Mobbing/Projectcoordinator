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
            IList<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            if (places.Count == 0)
            {
                return [];
            }

            var carCosts = carRouteService.CalculateRoutesAsync(places, originLatitude, originLongitude).ContinueWith(r => r.Result.ToDictionary(r => r.PlaceId, r => r.CostCents));

            var departureTime = GetNextWeekdayStartTime();

            var routeTasks = places.Select(async place =>
            {
                try
                {
                    var outboundTask = CalculateSingleRouteAsync(
                        originLatitude, originLongitude,
                        place.Latitude, place.Longitude,
                        departureTime);

                    var returnTripTask = CalculateSingleRouteAsync(
                        place.Latitude, place.Longitude,
                        originLatitude, originLongitude,
                        departureTime);

                    // Wait for both calls to complete
                    await Task.WhenAll(outboundTask, returnTripTask);

                    var outbound = await outboundTask;
                    var returnTrip = await returnTripTask;

                    var averageDuration = (outbound + returnTrip) / 2;
                    var costCents = (await carCosts).TryGetValue(place.Id, out var cost) ? cost : 0;

                    return new TrainRouteResult
                    {
                        PlaceId = place.Id,
                        DurationSeconds = averageDuration,
                        CostCents = costCents
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to calculate train route for place {PlaceId} ({PlaceName})",
                        place.Id, place.Name);
                    throw;
                }
            });

            return await Task.WhenAll(routeTasks);
        }

        private async Task<uint> CalculateSingleRouteAsync(
            double fromLat, double fromLon,
            double toLat, double toLon,
            DateTimeOffset time)
        {
            var fromPlace = $"{fromLat.ToString(CultureInfo.InvariantCulture)},{fromLon.ToString(CultureInfo.InvariantCulture)}";
            var toPlace = $"{toLat.ToString(CultureInfo.InvariantCulture)},{toLon.ToString(CultureInfo.InvariantCulture)}";
            var timeParam = time.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

            var url = $"/api/v5/plan?fromPlace={fromPlace}&toPlace={toPlace}&time={Uri.EscapeDataString(timeParam)}&detailedTransfers=false&maxPreTransitTime=1800&maxPostTransitTime=1800";

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

                if (result == null)
                {
                    logger.LogWarning("No response from Transitous API: {Content}",
                        responseBody);
                    return 0;
                }

                if (result.Itineraries.Count == 0 && result.Direct.Count == 0)
                {
                    logger.LogWarning("No train or direct routes found");
                    return 0;
                }

                // Calculate averages for both transit and direct routes
                var transitAvg = result.Itineraries.Count > 0
                    ? result.Itineraries.Average(i => i.Duration)
                    : uint.MaxValue;

                var directAvg = result.Direct.Count > 0
                    ? result.Direct.Average(i => i.Duration)
                    : uint.MaxValue;

                return (uint)Math.Ceiling(Math.Min(transitAvg, directAvg));
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

        private static DateTimeOffset GetNextWeekdayStartTime()
        {
            var now = DateTimeOffset.UtcNow;
            var candidate = new DateTimeOffset(now.Year, now.Month, now.Day, 10, 0, 0, TimeSpan.Zero);

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

        private sealed record TransitousPlanResponse(List<TransitousItinerary> Itineraries, List<TransitousItinerary> Direct);

        // The duration is in seconds
        private sealed record TransitousItinerary(uint Duration);
    }
}
