using api.Entities;
using api.Exceptions;
using api.Models;
using api.Repositories;
using api.Services;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Services;

public class PlaceCosmosServiceTests
{
    public class GetAllPlacesAsync
    {
        [Test]
        public async Task ReturnsAllPlaces_WhenPlacesExist()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();

            var placeEntities = new List<PlaceEntity>
            {
                new PlaceEntity { Id = new PlaceId("1"), UserId = new UserId(Guid.NewGuid()), Name = "Place 1", Latitude = 1.0, Longitude = 1.0 },
                new PlaceEntity { Id = new PlaceId("2"), UserId = new UserId(Guid.NewGuid()), Name = "Place 2", Latitude = 2.0, Longitude = 2.0 }
            };

            mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(placeEntities);

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act
            var result = await service.GetAllPlacesAsync();

            // Assert
            var places = result.ToList();
            await Assert.That(places).HasCount().EqualTo(2);
            await Assert.That(places[0].Name).IsEqualTo("Place 1");
            await Assert.That(places[1].Name).IsEqualTo("Place 2");
        }

        [Test]
        public async Task ReturnsEmptyList_WhenNoPlacesExist()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();

            mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlaceEntity>());

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act
            var result = await service.GetAllPlacesAsync();

            // Assert
            await Assert.That(result).IsEmpty();
        }
    }

    public class GetPlacesByUserIdAsync
    {
        [Test]
        public async Task ReturnsUserPlaces_WhenPlacesExist()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();
            var userId = new UserId(Guid.NewGuid());

            var placeEntities = new List<PlaceEntity>
            {
                new PlaceEntity { Id = new PlaceId("1"), UserId = userId, Name = "User Place 1", Latitude = 1.0, Longitude = 1.0 },
                new PlaceEntity { Id = new PlaceId("2"), UserId = userId, Name = "User Place 2", Latitude = 2.0, Longitude = 2.0 }
            };

            mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(placeEntities);

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act
            var result = await service.GetPlacesByUserIdAsync(userId);

            // Assert
            var places = result.ToList();
            await Assert.That(places).HasCount().EqualTo(2);
            await Assert.That(places[0].UserId).IsEqualTo(userId);
            await Assert.That(places[1].UserId).IsEqualTo(userId);
        }

        [Test]
        public async Task ReturnsEmptyList_WhenUserHasNoPlaces()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();
            var userId = new UserId(Guid.NewGuid());

            mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<PlaceEntity>());

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act
            var result = await service.GetPlacesByUserIdAsync(userId);

            // Assert
            await Assert.That(result).IsEmpty();
        }
    }

    public class AddPlace
    {
        [Test]
        public async Task AddsPlace_WhenCoordinatesAreValid()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();
            var userId = new UserId(Guid.NewGuid());

            var placeRequest = new PlaceRequest
            {
                UserId = userId,
                Name = "New Place",
                Latitude = 10.5,
                Longitude = 20.5
            };

            mockTripService.Setup(t => t.ValidateAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(true);
            mockRepository.Setup(r => r.AddAsync(It.IsAny<PlaceEntity>()))
                .ReturnsAsync((PlaceEntity pe) => pe);

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act
            var result = await service.AddPlace(placeRequest);

            // Assert
            await Assert.That(result.Name).IsEqualTo("New Place");
            await Assert.That(result.UserId).IsEqualTo(userId);
            mockTripService.Verify(t => t.ValidateAsync(10.5, 20.5), Times.Once);
            mockRepository.Verify(r => r.AddAsync(It.IsAny<PlaceEntity>()), Times.Once);
        }

        [Test]
        public async Task ThrowsProblemDetailsException_WhenCoordinatesAreInvalid()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();

            var placeRequest = new PlaceRequest
            {
                UserId = new UserId(Guid.NewGuid()),
                Name = "Invalid Place",
                Latitude = 999.0,
                Longitude = 999.0
            };

            mockTripService.Setup(t => t.ValidateAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(false);

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ProblemDetailsException>(async () => await service.AddPlace(placeRequest));
            mockRepository.Verify(r => r.AddAsync(It.IsAny<PlaceEntity>()), Times.Never);
        }
    }

    public class UpdatePlace
    {
        [Test]
        public async Task UpdatesPlace_WhenCoordinatesAreValid()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();
            var placeId = new PlaceId("place-123");
            var userId = new UserId(Guid.NewGuid());

            var placeRequest = new PlaceRequest
            {
                UserId = userId,
                Name = "Updated Place",
                Latitude = 15.5,
                Longitude = 25.5
            };

            mockTripService.Setup(t => t.ValidateAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(true);
            mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PlaceEntity>()))
                .ReturnsAsync((PlaceEntity pe) => pe);

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act
            var result = await service.UpdatePlace(placeId, placeRequest);

            // Assert
            await Assert.That(result.Id).IsEqualTo(placeId);
            await Assert.That(result.Name).IsEqualTo("Updated Place");
            await Assert.That(result.UserId).IsEqualTo(userId);
            mockTripService.Verify(t => t.ValidateAsync(15.5, 25.5), Times.Once);
            mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PlaceEntity>()), Times.Once);
        }

        [Test]
        public async Task ThrowsProblemDetailsException_WhenCoordinatesAreInvalid()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();
            var placeId = new PlaceId("place-123");

            var placeRequest = new PlaceRequest
            {
                UserId = new UserId(Guid.NewGuid()),
                Name = "Invalid Update",
                Latitude = 999.0,
                Longitude = 999.0
            };

            mockTripService.Setup(t => t.ValidateAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(false);

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ProblemDetailsException>(async () => await service.UpdatePlace(placeId, placeRequest));
            mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PlaceEntity>()), Times.Never);
        }
    }

    public class DeletePlace
    {
        [Test]
        public async Task DeletesPlaceSuccessfully()
        {
            // Arrange
            var mockRepository = new Mock<IPlaceRepository>();
            var mockTripService = new Mock<ITripService>();
            var placeId = new PlaceId("place-123");
            var userId = new UserId(Guid.NewGuid());

            mockRepository.Setup(r => r.DeleteAsync(placeId, userId))
                .Returns(Task.CompletedTask);

            var service = new PlaceCosmosService(mockRepository.Object, mockTripService.Object);

            // Act
            await service.DeletePlace(placeId, userId);

            // Assert
            mockRepository.Verify(r => r.DeleteAsync(placeId, userId), Times.Once);
        }
    }
}
