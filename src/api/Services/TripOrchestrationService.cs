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

        public TripOrchestrationService(
            IPlaceRepository repository,
            ICarRouteService carRouteService,
            ITrainRouteService trainRouteService)
        {
            this.repository = repository;
            this.carRouteService = carRouteService;
            this.trainRouteService = trainRouteService;
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude)
        {
            var places = await repository.GetAllAsync();

            if (!places.Any())
            {
                return [];
            }

            var trainPlaces = new List<PlaceEntity>();
            var carPlaces = new List<PlaceEntity>();

            foreach (var place in places)
            {
                if (place.TransportMode == TransportMode.Train)
                {
                    trainPlaces.Add(place);
                }
                else if (place.TransportMode == TransportMode.Car)
                {
                    carPlaces.Add(place);
                }
            }

            var carTrips = CalculateCarTripsAsync(carPlaces, latitude, longitude);
            var trainTrips = CalculateTrainTripsAsync(trainPlaces, latitude, longitude);

            await Task.WhenAll(carTrips, trainTrips);

            return (await carTrips).Concat(await trainTrips);
        }

        private async Task<IEnumerable<Trip>> CalculateTrainTripsAsync(List<PlaceEntity> trainPlaces, double latitude, double longitude)
        {
            if (trainPlaces.Count == 0)
            {
                return [];
            }
            var trainRoutes = await trainRouteService.CalculateRoutesAsync(trainPlaces, latitude, longitude);

            return trainRoutes.Select((r, i) =>
            {
                var place = trainPlaces[i].Id == r.PlaceId ? trainPlaces[i] : trainPlaces.First(p => p.Id == r.PlaceId);
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
        }
        private async Task<IEnumerable<Trip>> CalculateCarTripsAsync(List<PlaceEntity> carPlaces, double latitude, double longitude)
        {
            if (carPlaces.Count == 0)
            {
                return [];
            }
            var carRoutes = await carRouteService.CalculateRoutesAsync(carPlaces, latitude, longitude);

            return carRoutes.Select((r, i) =>
            {
                var place = carPlaces[i].Id == r.PlaceId ? carPlaces[i] : carPlaces.First(p => p.Id == r.PlaceId);
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
        }
    }
}
