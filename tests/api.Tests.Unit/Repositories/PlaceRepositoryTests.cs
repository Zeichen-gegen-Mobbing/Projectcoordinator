using api.Entities;
using api.Options;
using api.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Repositories;

public class PlaceRepositoryTests
{
    private readonly Mock<CosmosClient> mockCosmosClient = new Mock<CosmosClient>();
    private readonly Mock<IOptions<CosmosOptions>> mockOptions = new Mock<IOptions<CosmosOptions>>();
    private readonly Mock<ILogger<PlaceRepository>> mockLogger = new Mock<ILogger<PlaceRepository>>();
    private readonly Mock<Container> mockContainer = new Mock<Container>();
    private readonly PlaceRepository repository;


    public PlaceRepositoryTests()
    {
        mockOptions.Setup(o => o.Value).Returns(new CosmosOptions
        {
            Title = "TestCosmos",
            ConnectionString = "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=test-key==",
            DatabaseId = "TestDb",
            PlacesContainerId = "TestContainer",
            UsersContainerId = "TestUserContainer"
        });

        mockCosmosClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockContainer.Object);

        mockContainer.Setup(c => c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ContainerResponse>());

        repository = new PlaceRepository(mockCosmosClient.Object, mockOptions.Object, mockLogger.Object);
    }

    public class DeleteAsync : PlaceRepositoryTests
    {
        [Test]
        public async Task CallsDeleteItemAsync_WithCorrectIdAndPartitionKey()
        {
            // Arrange           
            var userId = UserId.Parse("00000000-0000-0000-0000-000000000001");
            var placeId = new PlaceId("place123");

            mockContainer.Setup(c => c.DeleteItemAsync<PlaceEntity>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<ItemResponse<PlaceEntity>>());

            // Act
            await repository.DeleteAsync(userId, placeId);

            // Assert
            mockContainer.Verify(c => c.DeleteItemAsync<PlaceEntity>(
                placeId.Value,
                It.Is<PartitionKey>(pk => pk.ToString().Contains(userId.Value.ToString())),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
