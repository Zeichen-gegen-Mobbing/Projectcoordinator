using api.Entities;
using api.Models;
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

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude)
        {
            var places = await repository.GetAllAsync();

            if (!places.Any())
            {
                return Enumerable.Empty<Trip>();
            }

            var carPlaces = places.Where(p => p.TransportMode == TransportMode.Car);
            var trainPlaces = places.Where(p => p.TransportMode == TransportMode.Train);

            Task<IEnumerable<CarRouteResult>> carTask = carRouteService.CalculateRoutesAsync(carPlaces.Concat(trainPlaces), latitude, longitude);
            Task<IEnumerable<TrainRouteResult>> trainTask;

            if (trainPlaces.Any())
            {
                async Task<Dictionary<PlaceId, ushort>> carCostsTask()
                {
                    var carTrips = await carTask;
                    return carTrips.ToDictionary(r => r.PlaceId, r => r.CostCents);
                }
                trainTask = trainRouteService.CalculateRoutesAsync(
                    trainPlaces, latitude, longitude, carCostsTask());
            }
            else
            {
                trainTask = Task.FromResult(Enumerable.Empty<TrainRouteResult>());
            }

            await Task.WhenAll(carTask, trainTask);

            var carResults = await carTask;
            var trainResults = await trainTask;

            var carTrips = carResults
                .Where(r => carPlaces.Any(p => p.Id == r.PlaceId))
                .Select(r =>
                {
                    var place = carPlaces.First(p => p.Id == r.PlaceId);
                    return new Trip
                    {
                        Place = new Place
                        {
                            Id = place.Id,
                            Name = place.Name,
                            UserId = place.UserId,
                            TransportMode = TransportMode.Car
                        },
                        Time = TimeSpan.FromSeconds(r.DurationSeconds),
                        Cost = r.CostCents
                    };
                });

            var trainTrips = trainResults.Select(r =>
            {
                var place = trainPlaces.First(p => p.Id == r.PlaceId);
                return new Trip
                {
                    Place = new Place
                    {
                        Id = place.Id,
                        Name = place.Name,
                        UserId = place.UserId,
                        TransportMode = TransportMode.Train
                    },
                    Time = TimeSpan.FromSeconds(r.DurationSeconds),
                    Cost = r.CostCents
                };
            });

            return carTrips.Concat(trainTrips);
        }
    }
}
