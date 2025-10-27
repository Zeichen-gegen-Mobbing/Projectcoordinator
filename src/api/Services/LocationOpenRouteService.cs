using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using api.Exceptions;
using api.Extensions;
using api.Models;
using api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public sealed class LocationOpenRouteService : ILocationService
    {
        private readonly HttpClient client;
        private readonly ILogger<LocationOpenRouteService> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public LocationOpenRouteService(IOptions<OpenRouteServiceOptions> options, IHttpClientFactory clientFactory, ILogger<LocationOpenRouteService> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public async Task<IEnumerable<LocationSearchResult>> SearchAsync(string query)
        {
            var requestUri = $"geocode/search?text={Uri.EscapeDataString(query)}&boundary.country=DEU";
            var response = await client.GetAsync(requestUri);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<OpenRouteServiceGeocodeResponse>(responseBody, _serializeOptions);
                    return result.Features.Select(feature => new LocationSearchResult
                    {
                        Label = feature.Properties.Label,
                        Longitude = feature.Geometry.Coordinates[0],
                        Latitude = feature.Geometry.Coordinates[1],
                        Name = feature.Properties.Name,
                        Street = feature.Properties.Street,
                        HouseNumber = feature.Properties.Housenumber,
                        PostalCode = feature.Properties.Postalcode,
                        Country = feature.Properties.Country,
                        Region = feature.Properties.Region,
                        County = feature.Properties.County,
                        Locality = feature.Properties.Locality
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Something went wrong while deserializing Geocode response from ORS: {Content}", responseBody);
                    throw new ProblemDetailsException(System.Net.HttpStatusCode.InternalServerError, "Internal Server Error", "Something went wrong while getting Geocode results from ORS");
                }
            }
            else
            {
                logger.LogError("Something went wrong while getting Geocode results from ORS {Status}: {Content}", response.StatusCode, responseBody);
                throw new ProblemDetailsException(System.Net.HttpStatusCode.InternalServerError, "Internal Server Error", "Something went wrong while getting Geocode results from ORS");
            }
        }

        public async Task<bool> ValidateAsync(double latitude, double longitude)
        {
            var response = await client.PostAsJsonAsync("v2/snap/driving-car", new
            {
                locations = new[] { new[] { longitude, latitude } },
                radius = 350
            });
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                logger.LogDebug("Response from API: {Response}", responseBody);
                var result = JsonSerializer.Deserialize<OpenRouteServiceSnapResponse>(responseBody, _serializeOptions);
                return result.Locations.Length > 0;
            }
            else
            {
                logger.LogError("Something went wrong while getting Snap from ORS {Status}: {Content}", response.StatusCode, responseBody);
                throw new ProblemDetailsException(System.Net.HttpStatusCode.InternalServerError, "Internal Server Error", "Something went wrong while getting Snap from ORS");
            }
        }

        public struct OpenRouteServiceSnapResponse
        {
            [Required]
            public required SnappedLocation[] Locations { get; init; }
        }

        public struct SnappedLocation
        {
            [Required]
            public required double[] Location { get; init; }
        }
    }
}
