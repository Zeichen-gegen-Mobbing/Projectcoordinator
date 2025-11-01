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
        private readonly ILogger<TrainTransitousService> logger;

        public TrainTransitousService(
            IHttpClientFactory clientFactory,
            ILogger<TrainTransitousService> logger)
        {
            client = clientFactory.CreateClient();
            this.logger = logger;
        }

        public Task<IEnumerable<TrainRouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude,
            Task<Dictionary<PlaceId, ushort>> carCosts)
        {
            throw new NotImplementedException("TrainTransitousService.CalculateRoutesAsync not yet implemented");
        }
    }
}
