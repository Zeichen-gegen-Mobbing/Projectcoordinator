using api.Entities;
using api.Models;
using api.Repositories;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;
using System.Linq;

namespace api.Services
{
    /// <summary>
    /// Orchestrates trip calculations by delegating to car and train route services.
    /// </summary>
    public sealed class TripOrchestrationService(
        IPlaceRepository repository,
        ICarRouteService carRouteService,
        ITrainRouteService trainRouteService,
        ICostCalculationService costCalculationService) : ITripService
    {
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

            return await carTrips.Concat(trainTrips).ToArrayAsync();
        }

        private async IAsyncEnumerable<Trip> CalculateTrainTripsAsync(List<PlaceEntity> trainPlaces, double latitude, double longitude)
        {
            if (trainPlaces.Count == 0)
            {
                yield break;
            }

            var carRoutesEnumerator = carRouteService.CalculateRoutesAsync(trainPlaces, latitude, longitude).GetAsyncEnumerator();
            var trainRoutesEnumerator = trainRouteService.CalculateRoutesAsync(trainPlaces, latitude, longitude).GetAsyncEnumerator();

            while (true)
            {
                var carHasNext = carRoutesEnumerator.MoveNextAsync().AsTask();
                var trainHasNext = trainRoutesEnumerator.MoveNextAsync().AsTask();

                await Task.WhenAll(carHasNext, trainHasNext);
                if (!carHasNext.Result || !trainHasNext.Result)
                    yield break;

                yield return new Trip
                {
                    Place = new Place
                    {
                        Id = trainRoutesEnumerator.Current.Place.Id,
                        Name = trainRoutesEnumerator.Current.Place.Name,
                        UserId = trainRoutesEnumerator.Current.Place.UserId,
                        TransportMode = TransportMode.Train
                    },
                    Time = TimeSpan.FromSeconds(trainRoutesEnumerator.Current.DurationSeconds),
                    Cost = await costCalculationService.CalculateCostAsync(
                        trainRoutesEnumerator.Current.Place.UserId,
                        carRoutesEnumerator.Current.DistanceMeters,
                        trainRoutesEnumerator.Current.DurationSeconds)
                };
            }
        }

        private async IAsyncEnumerable<Trip> CalculateCarTripsAsync(List<PlaceEntity> carPlaces, double latitude, double longitude)
        {
            if (carPlaces.Count == 0)
            {
                yield break;
            }

            await foreach (var carRoute in carRouteService.CalculateRoutesAsync(carPlaces, latitude, longitude))
            {
                yield return new Trip
                {
                    Place = new Place
                    {
                        Id = carRoute.Place.Id,
                        Name = carRoute.Place.Name,
                        UserId = carRoute.Place.UserId,
                        TransportMode = TransportMode.Car
                    },
                    Time = TimeSpan.FromSeconds(carRoute.DurationSeconds),
                    Cost = await costCalculationService.CalculateCostAsync(carRoute.Place.UserId, carRoute.DistanceMeters, carRoute.DurationSeconds)
                };
            }
        }
    }
}
