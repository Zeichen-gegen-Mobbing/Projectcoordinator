using System.Text.Json;
using api.Exceptions;
using api.Extensions;
using api.Models;
using api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public sealed class LocationSearchOpenRouteService : ILocationSearchService
    {
        private readonly HttpClient client;
        private readonly ILogger<LocationSearchOpenRouteService> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public LocationSearchOpenRouteService(IOptions<OpenRouteServiceOptions> options, IHttpClientFactory clientFactory, ILogger<LocationSearchOpenRouteService> logger)
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
    }
}
