using api.Entities;
using api.Models;
using api.Repositories;
using api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Services;

public class PlaceCosmosServiceTests
{
    private readonly Mock<IPlaceRepository> mockRepository = new Mock<IPlaceRepository>();
    private readonly Mock<ILocationService> mockLocationService = new Mock<ILocationService>();
    private readonly PlaceCosmosService service;

    public PlaceCosmosServiceTests()
    {
        mockLocationService.Setup(s => s.ValidateAsync(It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(true);
        service = new PlaceCosmosService(mockRepository.Object, mockLocationService.Object);
    }

    public class DeletePlace : PlaceCosmosServiceTests
    {
        [Test]
        public async Task CallsRepositoryDeleteAsync_WithUserIdAndPlaceId()
        {
            // Arrange
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            var placeId = new PlaceId("place123");

            // Act
            await service.DeletePlace(userId, placeId);

            // Assert
            mockRepository.Verify(r => r.DeleteAsync(userId, placeId), Times.Once);
        }
    }

    public class GetAllPlacesAsync : PlaceCosmosServiceTests
    {
        [Test]
        public async Task ReturnsPlaces_WhenRepositoryHasData()
        {
            // Arrange
            var entities = new List<PlaceEntity>
            {
                new PlaceEntity
                {
                    Id = new PlaceId("place1"),
                    UserId = UserId.Parse("00000000-0000-0000-0000-000000000001"),
                    Name = "Place 1",
                    Latitude = 52.5200,
                    Longitude = 13.4050
                },
                new PlaceEntity
                {
                    Id = new PlaceId("place2"),
                    UserId = UserId.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = "Place 2",
                    Latitude = 51.5074,
                    Longitude = -0.1278
                }
            };

            mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);

            // Act
            var result = await service.GetAllPlacesAsync();

            // Assert
            var places = result.ToList();
            await Assert.That(places).HasCount().EqualTo(2);
            await Assert.That(places[0].Name).IsEqualTo("Place 1");
            await Assert.That(places[1].Name).IsEqualTo("Place 2");
        }

        [Test]
        public async Task ReturnsEmptyList_WhenRepositoryHasNoData()
        {
            // Arrange
            mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

            // Act
            var result = await service.GetAllPlacesAsync();

            // Assert
            await Assert.That(result).IsEmpty();
        }
    }

    public class AddPlace : PlaceCosmosServiceTests
    {
        [Test]
        public async Task CallsRepositoryAddAsync_WithCorrectEntity()
        {
            // Arrange
            var placeRequest = new Models.PlaceRequest
            {
                UserId = UserId.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "New Place",
                Latitude = 52.5200,
                Longitude = 13.4050,
                TransportMode = TransportMode.Car
            };

            var addedEntity = new PlaceEntity
            {
                Id = new PlaceId("newplace123"),
                UserId = placeRequest.UserId,
                Name = placeRequest.Name,
                Latitude = placeRequest.Latitude,
                Longitude = placeRequest.Longitude
            };

            mockRepository.Setup(r => r.AddAsync(It.IsAny<PlaceEntity>()))
                .ReturnsAsync(addedEntity);

            // Act
            var result = await service.AddPlace(placeRequest);

            // Assert
            await Assert.That(result.Name).IsEqualTo(placeRequest.Name);
            await Assert.That(result.UserId).IsEqualTo(placeRequest.UserId);
            mockRepository.Verify(r => r.AddAsync(
                It.Is<PlaceEntity>(e =>
                    e.UserId == placeRequest.UserId &&
                    e.Name == placeRequest.Name &&
                    e.Latitude == placeRequest.Latitude &&
                    e.Longitude == placeRequest.Longitude)),
                Times.Once);
        }
    }
}
