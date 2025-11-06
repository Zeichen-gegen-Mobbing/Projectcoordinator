using api.Entities;
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

        public TrainTransitousService(
            IHttpClientFactory clientFactory,
            ICarRouteService carRouteService,
            ILogger<TrainTransitousService> logger)
        {
            client = clientFactory.CreateClient();
            this.carRouteService = carRouteService;
            this.logger = logger;
        }

        public async Task<IEnumerable<TrainRouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            var carRoutes = await carRouteService.CalculateRoutesAsync(places, originLatitude, originLongitude);
            var carCosts = carRoutes.ToDictionary(r => r.PlaceId, r => r.CostCents);
            
            throw new NotImplementedException("TrainTransitousService.CalculateRoutesAsync not yet implemented");
        }
    }
}
