using api.Entities;
using api.Extensions;
using api.Models;
using api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Services
{
    /// <summary>
    /// Calculates car routes using OpenRouteService driving-car profile.
    /// </summary>
    public sealed class CarOpenRouteService : ICarRouteService
    {
        private readonly HttpClient client;
        private readonly ILogger<CarOpenRouteService> logger;

        public CarOpenRouteService(
            IHttpClientFactory clientFactory,
            IOptions<OpenRouteServiceOptions> options,
            ILogger<CarOpenRouteService> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public Task<IEnumerable<CarRouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            throw new NotImplementedException("CarOpenRouteService.CalculateRoutesAsync not yet implemented");
        }
    }
}
