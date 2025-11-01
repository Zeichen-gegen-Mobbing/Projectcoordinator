using api.Repositories;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Orchestrates trip calculations by delegating to car and train route services.
    /// </summary>
    public sealed class TripOrchestrationService : ITripService
    {
        private readonly IPlaceRepository repository;
        private readonly ICarRouteService carRouteService;
        private readonly ITrainRouteService trainRouteService;
        private readonly ILogger<TripOrchestrationService> logger;

        public TripOrchestrationService(
            IPlaceRepository repository,
            ICarRouteService carRouteService,
            ITrainRouteService trainRouteService,
            ILogger<TripOrchestrationService> logger)
        {
            this.repository = repository;
            this.carRouteService = carRouteService;
            this.trainRouteService = trainRouteService;
            this.logger = logger;
        }

        public Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude)
        {
            throw new NotImplementedException("TripOrchestrationService.GetAllTripsAsync not yet implemented");
        }
    }
}
