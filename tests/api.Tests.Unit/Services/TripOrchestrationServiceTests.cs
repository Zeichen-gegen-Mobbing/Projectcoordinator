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

        private static CarRouteResult CreateCarResult(string placeId, double duration, double distance, uint cost) => new()
        {
            PlaceId = PlaceId.Parse(placeId),
            DurationSeconds = (uint)Math.Ceiling(duration),
            DistanceMeters = (uint)Math.Ceiling(distance),
            CostCents = cost
        };

        private static TrainRouteResult CreateTrainResult(string placeId, double duration, uint cost) => new()
        {
            PlaceId = PlaceId.Parse(placeId),
            DurationSeconds = (uint)Math.Ceiling(duration),
            CostCents = cost
        };

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
                CreateCarResult("place1", 600, 5000, 150),
                CreateCarResult("place2", 900, 10000, 300)
            };

            var (service, carServiceMock, trainServiceMock, _) = CreateService(places, carResults);

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
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never);
        }

        /// <summary>
        /// Given: All places have Train transport mode
        /// When: Getting all trips
        /// Then: Uses only train service (which internally calls car service), returns trips with train times and car costs
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
                new TrainRouteResult
                {
                    PlaceId = PlaceId.Parse("place1"),
                    DurationSeconds = 800,
                    CostCents = 150
                },
                new TrainRouteResult
                {
                    PlaceId = PlaceId.Parse("place2"),
                    DurationSeconds = 1200,
                    CostCents = 300
                }
            };

            var (service, carServiceMock, trainServiceMock, _) = CreateService(places, new List<CarRouteResult>(), trainResults);

            // Act
            var trips = (await service.GetAllTripsAsync(52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(trips.Count).IsEqualTo(2);

            var trip1 = trips.First(t => t.Place.Id == places[0].Id);
            await Assert.That(trip1.Time).IsEqualTo(TimeSpan.FromSeconds(800)); // Train time
            await Assert.That(trip1.Cost).IsEqualTo((ushort)150); // Car cost
            await Assert.That(trip1.Place.TransportMode).IsEqualTo(TransportMode.Train);

            var trip2 = trips.First(t => t.Place.Id == places[1].Id);
            await Assert.That(trip2.Time).IsEqualTo(TimeSpan.FromSeconds(1200)); // Train time
            await Assert.That(trip2.Cost).IsEqualTo((ushort)300); // Car cost

            // Verify only train service was called by orchestrator (train service calls car service internally)
            carServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never);

            trainServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
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
                new CarRouteResult
                {
                    PlaceId = PlaceId.Parse("place1"),
                    DurationSeconds = 600,
                    DistanceMeters = 5000,
                    CostCents = 150
                },
                new CarRouteResult
                {
                    PlaceId = PlaceId.Parse("place3"),
                    DurationSeconds = 400,
                    DistanceMeters = 3000,
                    CostCents = 90
                }
            };

            var trainResults = new List<TrainRouteResult>
            {
                new TrainRouteResult
                {
                    PlaceId = PlaceId.Parse("place2"),
                    DurationSeconds = 1200,
                    CostCents = 300
                }
            };

            var (service, _, _, _) = CreateService(places, carResults, trainResults);

            // Act
            var trips = (await service.GetAllTripsAsync(52.5100, 13.4000)).ToList();

            // Assert
            await Assert.That(trips.Count).IsEqualTo(3);

            var carTrip1 = trips.First(t => t.Place.Id == PlaceId.Parse("place1"));
            await Assert.That(carTrip1.Time).IsEqualTo(TimeSpan.FromSeconds(600)); // Car time
            await Assert.That(carTrip1.Cost).IsEqualTo((ushort)150);

            var trainTrip = trips.First(t => t.Place.Id == PlaceId.Parse("place2"));
            await Assert.That(trainTrip.Time).IsEqualTo(TimeSpan.FromSeconds(1200)); // Train time
            await Assert.That(trainTrip.Cost).IsEqualTo((ushort)300); // Car cost

            var carTrip2 = trips.First(t => t.Place.Id == PlaceId.Parse("place3"));
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
            var (service, carServiceMock, trainServiceMock, _) = CreateService(places, new List<CarRouteResult>());

            // Act
            var trips = await service.GetAllTripsAsync(52.5100, 13.4000);

            // Assert
            await Assert.That(trips.Count()).IsEqualTo(0);

            // Verify no services were called
            carServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()),
                Times.Never);

            trainServiceMock.Verify(
                s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
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
                new CarRouteResult
                {
                    PlaceId = PlaceId.Parse("place1"),
                    DurationSeconds = 600,
                    DistanceMeters = 5000,
                    CostCents = 150
                }
            };

            var trainResults = new List<TrainRouteResult>
            {
                new TrainRouteResult
                {
                    PlaceId = PlaceId.Parse("place2"),
                    DurationSeconds = 800,
                    CostCents = 150
                }
            };

            var carExecutionTime = DateTime.MinValue;
            var trainExecutionTime = DateTime.MinValue;

            var repositoryMock = new Mock<IPlaceRepository>();
            repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(places);

            var carServiceMock = new Mock<ICarRouteService>();
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(async () =>
                {
                    carExecutionTime = DateTime.UtcNow;
                    await Task.Delay(100); // Simulate API delay
                    return carResults;
                });

            var trainServiceMock = new Mock<ITrainRouteService>();
            trainServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(async () =>
                {
                    trainExecutionTime = DateTime.UtcNow;
                    await Task.Delay(100); // Simulate API delay
                    return trainResults;
                });

            var loggerMock = new Mock<ILogger<TripOrchestrationService>>();
            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                loggerMock.Object);

            // Act
            await service.GetAllTripsAsync(52.5100, 13.4000);

            // Assert - Both should start around same time (within 50ms)
            await Assert.That(trainExecutionTime).IsNotEqualTo(DateTime.MinValue);
            await Assert.That(carExecutionTime).IsNotEqualTo(DateTime.MinValue);
            var timeDifference = Math.Abs((trainExecutionTime - carExecutionTime).TotalMilliseconds);
            await Assert.That(timeDifference < 50).IsTrue();
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
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .ThrowsAsync(new InvalidOperationException("Car service failed"));

            var trainServiceMock = new Mock<ITrainRouteService>();
            var loggerMock = new Mock<ILogger<TripOrchestrationService>>();

            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                loggerMock.Object);

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
                new CarRouteResult
                {
                    PlaceId = PlaceId.Parse("place1"),
                    DurationSeconds = 600,
                    DistanceMeters = 5000,
                    CostCents = 150
                }
            };

            var repositoryMock = new Mock<IPlaceRepository>();
            repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(places);

            var carServiceMock = new Mock<ICarRouteService>();
            carServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .ReturnsAsync(carResults);

            var trainServiceMock = new Mock<ITrainRouteService>();
            trainServiceMock
                .Setup(s => s.CalculateRoutesAsync(
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .ThrowsAsync(new InvalidOperationException("Train service failed"));

            var loggerMock = new Mock<ILogger<TripOrchestrationService>>();

            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                loggerMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.GetAllTripsAsync(52.5100, 13.4000));

            await Assert.That(exception!.Message).IsEqualTo("Train service failed");
        }

        private static (
            TripOrchestrationService service,
            Mock<ICarRouteService> carServiceMock,
            Mock<ITrainRouteService> trainServiceMock,
            Mock<IPlaceRepository> repositoryMock)
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
                    It.IsAny<IEnumerable<PlaceEntity>>(),
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .ReturnsAsync(carResults);

            var trainServiceMock = new Mock<ITrainRouteService>();
            if (trainResults != null)
            {
                trainServiceMock
                    .Setup(s => s.CalculateRoutesAsync(
                        It.IsAny<IEnumerable<PlaceEntity>>(),
                        It.IsAny<double>(),
                        It.IsAny<double>()))
                    .ReturnsAsync(trainResults);
            }

            var loggerMock = new Mock<ILogger<TripOrchestrationService>>();

            var service = new TripOrchestrationService(
                repositoryMock.Object,
                carServiceMock.Object,
                trainServiceMock.Object,
                loggerMock.Object);

            return (service, carServiceMock, trainServiceMock, repositoryMock);
        }
    }
}
