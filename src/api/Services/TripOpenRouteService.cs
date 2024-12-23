using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Entities;
using api.Models;
using api.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public sealed class TripOpenRouteService(IPlaceRepository repository, IOptions<OpenRouteServiceOptions> options, IHttpClientFactory clientFactory, ILogger<TripOpenRouteService> logger) : ITripService
    {
        private readonly HttpClient client = clientFactory.CreateClient();
        public async Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude)
        {
            var places = await repository.GetAllAsync();
            if (places.Count() > 0)
            {
                client.BaseAddress = new Uri(options.Value.BaseUrl);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.PostAsJsonAsync("v2/matrix/driving-car", new
                {
                    locations = places.Select(place => new[] { place.Longitude, place.Latitude }).Append([longitude, latitude]).ToArray(),
                    destinations = Enumerable.Range(places.Count(),1),
                    sources = Enumerable.Range(0,places.Count()),
                    metrics = new[] { "duration", "distance" }
                });
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var serializeOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };
                    var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(responseBody, serializeOptions);
                    return places.Select((place, index) =>
                    {
                        return new Trip
                        {
                            PlaceId = place.Id,
                            Time = TimeSpan.FromSeconds(result.Durations[index].Single()),
                            Cost = (ushort)(Math.Ceiling(result.Distances[index].Single()) * 30)
                        };
                    });
                }
                else
                {
                    logger.LogError("Something went wrong while getting Matrix from ORS {Status}: {Content}", response.StatusCode, responseBody);
                    throw new Exception();
                }
            }
            else
            {
                return [];
            }
        }
    }
}