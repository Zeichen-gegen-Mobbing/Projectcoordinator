using api.Entities;
using api.Models;
using api.Repositories;
using api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Services;

public class TripOrchestrationServiceTests
{
    public class GetAllTripsAsync
    {
        private static PlaceEntity CreatePlace(string id, string name, TransportMode mode) => new()
        {
            Id = PlaceId.Parse(id),
            UserId = UserId.Parse(Guid.NewGuid().ToString()),
            Name = name,
            Latitude = 52.5200,
            Longitude = 13.4050,
            TransportMode = mode
        };

        private static CarRouteResult CreateCarResult(PlaceEntity place, double duration, double distance) => new()
        {
            Place = place,
            DurationSeconds = (uint)Math.Ceiling(duration),
            DistanceMeters = (uint)Math.Ceiling(distance)
        };

        private static TrainRouteResult CreateTrainResult(PlaceEntity place, double duration) => new()
        {
            Place = place,
            DurationSeconds = (uint)Math.Ceiling(duration)
        };

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Given: All places have Car transport mode
        /// When: Getting all trips
        /// Then: Uses only car route service and returns trips with car times and costs
        /// </summary>
        [Test]
        public async Task UsesOnlyCarService_WhenAllPlacesAreCar()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home", TransportMode.Car),
                CreatePlace("place2", "Office", TransportMode.Car)
            };

            var carResults = new List<CarRouteResult>
            {
                CreateCarResult(places[0], 600, 5000),
                CreateCarResult(places[1], 900, 10000)
            };

            var (service, _, trainServiceMock, _, costServiceMock) = CreateService(places, carResults);

            // Setup cost calculation
            costServiceMock.Setup(s => s.CalculateCostAsync(places[0].UserId, 5000, 600)).ReturnsAsync(150u);
            costServiceMock.Setup(s => s.CalculateCostAsync(places[1].UserId, 10000, 900)).ReturnsAsync(300u);

            // Act
            var trips = (await service.GetAllTripsAsync(52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(trips.Count).IsEqualTo(2);

            var trip1 = trips.First(t => t.Place.Id == places[0].Id);
            await Assert.That(trip1.Time).IsEqualTo(TimeSpan.FromSeconds(600));
            await Assert.That(trip1.Cost).IsEqualTo((ushort)150);
            await Assert.That(trip1.Place.TransportMode).IsEqualTo(TransportMode.Car);

            var trip2 = trips.First(t => t.Place.Id == places[1].Id);
            await Assert.That(trip2.Time).IsEqualTo(TimeSpan.FromSeconds(900));
            await Assert.That(trip2.Cost).IsEqualTo((ushort)300);

            // Verify train service was not called
            trainServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never);
        }

        /// <summary>
        /// Given: All places have Train transport mode
        /// When: Getting all trips
        /// Then: Uses train and car services, returns trips with train times and calculated costs
        /// </summary>
        [Test]
        public async Task UsesOnlyTrainService_WhenAllPlacesAreTrain()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home", TransportMode.Train),
                CreatePlace("place2", "Office", TransportMode.Train)
            };

            var trainResults = new List<TrainRouteResult>
            {
                CreateTrainResult(places[0], 800),
                CreateTrainResult(places[1], 1200)
            };

            var carResults = new List<CarRouteResult>
            {
                CreateCarResult(places[0], 600, 5000),
                CreateCarResult(places[1], 900, 10000)
            };

            var (service, carServiceMock, trainServiceMock, _, costServiceMock) = CreateService(places, carResults, trainResults);

            // Setup cost calculation - costs are based on car distance and train duration
            costServiceMock.Setup(s => s.CalculateCostAsync(places[0].UserId, 5000, 800)).ReturnsAsync(150u);
            costServiceMock.Setup(s => s.CalculateCostAsync(places[1].UserId, 10000, 1200)).ReturnsAsync(300u);

            // Act
            var trips = (await service.GetAllTripsAsync(52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(trips.Count).IsEqualTo(2);

            var trip1 = trips.First(t => t.Place.Id == places[0].Id);
            await Assert.That(trip1.Time).IsEqualTo(TimeSpan.FromSeconds(800)); // Train time
            await Assert.That(trip1.Cost).IsEqualTo((ushort)150); // Calculated cost
            await Assert.That(trip1.Place.TransportMode).IsEqualTo(TransportMode.Train);

            var trip2 = trips.First(t => t.Place.Id == places[1].Id);
            await Assert.That(trip2.Time).IsEqualTo(TimeSpan.FromSeconds(1200)); // Train time
            await Assert.That(trip2.Cost).IsEqualTo((ushort)300); // Calculated cost

            // Verify both services were called (train orchestrator needs car distances)
            carServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Once);

            trainServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    52.5100,
                    13.4000),
                Times.Once);
        }

        /// <summary>
        /// Given: Mixed places with both Car and Train modes
        /// When: Getting all trips
        /// Then: Returns correct times per mode with car costs for all
        /// </summary>
        [Test]
        public async Task HandlesMixedTransportModes_WhenPlacesHaveDifferentModes()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home", TransportMode.Car),
                CreatePlace("place2", "Office", TransportMode.Train),
                CreatePlace("place3", "Gym", TransportMode.Car)
            };

            var carResults = new List<CarRouteResult>
            {
                CreateCarResult(places[0], 600, 5000),
                CreateCarResult(places[2], 400, 3000)
            };

            var trainResults = new List<TrainRouteResult>
            {
                CreateTrainResult(places[1], 1200)
            };

            var (service, _, _, _, costServiceMock) = CreateService(places, carResults, trainResults);

            // Setup cost calculation - car mode uses distance and duration, train uses car distance and train duration
            costServiceMock.Setup(s => s.CalculateCostAsync(places[0].UserId, 5000, 600)).ReturnsAsync(150u);
            costServiceMock.Setup(s => s.CalculateCostAsync(places[2].UserId, 3000, 400)).ReturnsAsync(90u);
            costServiceMock.Setup(s => s.CalculateCostAsync(places[1].UserId, It.IsAny<uint>(), 1200)).ReturnsAsync(300u);

            // Act
            var trips = (await service.GetAllTripsAsync(52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(trips.Count).IsEqualTo(3);

            var carTrip1 = trips.First(t => t.Place.Id == places[0].Id);
            await Assert.That(carTrip1.Time).IsEqualTo(TimeSpan.FromSeconds(600)); // Car time
            await Assert.That(carTrip1.Cost).IsEqualTo((ushort)150);

            var trainTrip = trips.First(t => t.Place.Id == places[1].Id);
            await Assert.That(trainTrip.Time).IsEqualTo(TimeSpan.FromSeconds(1200)); // Train time
            await Assert.That(trainTrip.Cost).IsEqualTo((ushort)300); // Calculated cost

            var carTrip2 = trips.First(t => t.Place.Id == places[2].Id);
            await Assert.That(carTrip2.Time).IsEqualTo(TimeSpan.FromSeconds(400)); // Car time
            await Assert.That(carTrip2.Cost).IsEqualTo((ushort)90);
        }

        /// <summary>
        /// Given: No places exist in repository
        /// When: Getting all trips
        /// Then: Returns empty list without calling route services
        /// </summary>
        [Test]
        public async Task ReturnsEmpty_WhenNoPlacesExist()
        {
            // Arrange
            var places = new List<PlaceEntity>();
            var (service, carServiceMock, trainServiceMock, _, _) = CreateService(places, new List<CarRouteResult>());

            // Act
            var trips = await service.GetAllTripsAsync(52.5100, 13.4000);

            // Assert
            await Assert.That(trips.Count()).IsEqualTo(0);

            // Verify no services were called
            carServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never);

            trainServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never);
        }

        /// <summary>
        /// Given: Places with both car and train modes exist
        /// When: Car and train API calls can execute in parallel
        /// Then: Both services execute concurrently
        /// </summary>
        [Test]
        public async Task ExecutesServicesInParallel_WhenBothTransportModesExist()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home", TransportMode.Car),
                CreatePlace("place2", "Office", TransportMode.Train)
            };

            var carResults = new List<CarRouteResult>
            {
                CreateCarResult(places[0], 600, 5000)
            };

            var trainResults = new List<TrainRouteResult>
            {
                CreateTrainResult(places[1], 800)
            };

            var carExecutionTime = DateTime.MinValue;
            var trainExecutionTime = DateTime.MinValue;

            var repositoryMock = new Mock<IPlaceRepository>();
            repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(places);

            var carServiceMock = new Mock<ICarRouteService>();
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(() => CreateDelayedAsyncEnumerable(carResults, () => carExecutionTime = DateTime.UtcNow, 100));

            var trainServiceMock = new Mock<ITrainRouteService>();
            trainServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(() => CreateDelayedAsyncEnumerable(trainResults, () => trainExecutionTime = DateTime.UtcNow, 100));

            var costServiceMock = new Mock<ICostCalculationService>();
            costServiceMock.Setup(s => s.CalculateCostAsync(It.IsAny<UserId>(), It.IsAny<uint>(), It.IsAny<uint>()))
                .ReturnsAsync(150u);

            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                costServiceMock.Object);

            // Act
            await service.GetAllTripsAsync(52.5100, 13.4000);

            // Assert - Both should start around same time (within 50ms)
            await Assert.That(trainExecutionTime).IsNotEqualTo(DateTime.MinValue);
            await Assert.That(carExecutionTime).IsNotEqualTo(DateTime.MinValue);
            var timeDifference = Math.Abs((trainExecutionTime - carExecutionTime).TotalMilliseconds);
            await Assert.That(timeDifference < 50).IsTrue();
        }

        private static async IAsyncEnumerable<T> CreateDelayedAsyncEnumerable<T>(IEnumerable<T> source, Action onStart, int delayMs)
        {
            onStart();
            await Task.Delay(delayMs);
            foreach (var item in source)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Given: Car service throws exception
        /// When: Getting all trips
        /// Then: Exception propagates to caller
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenCarServiceFails()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home", TransportMode.Car)
            };

            var repositoryMock = new Mock<IPlaceRepository>();
            repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(places);

            var carServiceMock = new Mock<ICarRouteService>();
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(() => throw new InvalidOperationException("Car service failed"));

            var trainServiceMock = new Mock<ITrainRouteService>();

            var costServiceMock = new Mock<ICostCalculationService>();

            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                costServiceMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.GetAllTripsAsync(52.5100, 13.4000));

            await Assert.That(exception!.Message).IsEqualTo("Car service failed");
        }

        /// <summary>
        /// Given: Train service throws exception
        /// When: Getting all trips with train places
        /// Then: Exception propagates to caller
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenTrainServiceFails()
        {
            // Arrange
            var places = new List<PlaceEntity>
            {
                CreatePlace("place1", "Home", TransportMode.Train)
            };

            var carResults = new List<CarRouteResult>
            {
                CreateCarResult(places[0], 600, 5000)
            };

            var repositoryMock = new Mock<IPlaceRepository>();
            repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(places);

            var carServiceMock = new Mock<ICarRouteService>();
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(ToAsyncEnumerable(carResults));

            var trainServiceMock = new Mock<ITrainRouteService>();
            trainServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(() => throw new InvalidOperationException("Train service failed"));

            var costServiceMock = new Mock<ICostCalculationService>();

            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                costServiceMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.GetAllTripsAsync(52.5100, 13.4000));

            await Assert.That(exception!.Message).IsEqualTo("Train service failed");
        }

        private static (
            TripOrchestrationService service,
            Mock<ICarRouteService> carServiceMock,
            Mock<ITrainRouteService> trainServiceMock,
            Mock<IPlaceRepository> repositoryMock,
            Mock<ICostCalculationService> costServiceMock)
            CreateService(
                List<PlaceEntity> places,
                List<CarRouteResult> carResults,
                List<TrainRouteResult>? trainResults = null)
        {
            var repositoryMock = new Mock<IPlaceRepository>();
            repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(places);

            var carServiceMock = new Mock<ICarRouteService>();
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IList<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(ToAsyncEnumerable(carResults));

            var trainServiceMock = new Mock<ITrainRouteService>();
            if (trainResults != null)
            {
                trainServiceMock
                    .Setup(s => s.CalculateRoutesAsync(
                        It.IsAny<IList<PlaceEntity>>(),
                        It.IsAny<double>(),
                        It.IsAny<double>()))
                    .Returns(ToAsyncEnumerable(trainResults));
            }

            var costServiceMock = new Mock<ICostCalculationService>();

            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                costServiceMock.Object);

            return (service, carServiceMock, trainServiceMock, repositoryMock, costServiceMock);
        }
    }
}
