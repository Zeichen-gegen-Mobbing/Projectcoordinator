using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Entities;
using api.Exceptions;
using api.Models;
using api.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public sealed class TripOpenRouteService : ITripService
    {
        private readonly HttpClient client;
        private readonly IPlaceRepository repository;
        private readonly ILogger<TripOpenRouteService> logger;
        private static readonly string[] _metrics = ["duration", "distance"];
        private static readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public TripOpenRouteService(IPlaceRepository repository, IOptions<OpenRouteServiceOptions> options, IHttpClientFactory clientFactory, ILogger<TripOpenRouteService> logger)
        {
            client = clientFactory.CreateClient();
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            this.repository = repository;
            this.logger = logger;
        }
        public async Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude)
        {
            var places = await repository.GetAllAsync();
            if (places.Any())
            {
                var response = await client.PostAsJsonAsync("v2/matrix/driving-car", new
                {
                    locations = places.Select(place => new[] { place.Longitude, place.Latitude }).Append([longitude, latitude]).ToArray(),
                    destinations = Enumerable.Range(places.Count(), 1),
                    sources = Enumerable.Range(0, places.Count()),
                    metrics = _metrics
                });
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // TODO: Expose attribution to API
                    var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(responseBody, _serializeOptions);
                    return places.Select((place, index) =>
                    {
                        return new Trip
                        {
                            Place = new Place()
                            {
                                Id = place.Id,
                                Name = place.Name,
                                UserId = place.UserId,
                            },
                            Time = TimeSpan.FromSeconds(result.Durations[index].Single()),
                            Cost = (ushort)(Math.Ceiling(result.Distances[index].Single() / 1000) * 30)
                        };
                    });
                }
                else
                {
                    logger.LogError("Something went wrong while getting Matrix from ORS {Status}: {Content}", response.StatusCode, responseBody);
                    throw new ProblemDetailsException(System.Net.HttpStatusCode.InternalServerError, "Internal Server Error", "Something went wrong while getting Matrix from ORS");
                }
            }
            else
            {
                return [];
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
                return result.Locations.SingleOrDefault() != null;
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

            public required OrsLocation?[] Locations { get; set; }

            public struct OrsLocation
            {
                public required double[] Location { get; set; }
            }
        }
    }
}