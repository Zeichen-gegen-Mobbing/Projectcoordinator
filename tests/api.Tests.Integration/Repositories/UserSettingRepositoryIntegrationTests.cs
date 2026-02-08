using api.Options;
using api.Repositories;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Testcontainers.CosmosDb;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using ZgM.ProjectCoordinator.Shared;
using UserSettings = api.Models.UserSettings;

namespace api.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for UserSettingRepository that interact with Cosmos DB.
/// These tests use Testcontainers to run a CosmosDB emulator.
/// Note: CosmosDB emulator container takes 2-3 minutes to start on first run.
/// </summary>
public class UserSettingRepositoryIntegrationTests
{
    private static readonly CosmosDbContainer _cosmosContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
        .WithEnvironment("PROTOCOL", "https")
        .WithImagePullPolicy(PullPolicy.Always) // Image is time bound so always use newest.
        .Build();

    private static CosmosClient? _cosmosClient;

    private UserSettingRepository _repository = null!;

    [Before(Class)]
    public static async Task ClassSetup()
    {

        await _cosmosContainer.StartAsync();

        var cosmosClientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => _cosmosContainer.HttpClient
        };

        _cosmosClient = api.Factories.CosmosClientFactory.CreateCosmosClient(_cosmosContainer.GetConnectionString(), cosmosClientOptions);
    }

    [Before(Test)]
    public async Task Setup()
    {
        IOptions<CosmosOptions> cosmosOptions = Microsoft.Extensions.Options.Options.Create(new CosmosOptions
        {
            Title = "Section",
            ConnectionString = _cosmosContainer.GetConnectionString(),
            DatabaseId = Guid.NewGuid().ToString(),
            PlacesContainerId = "Places",
            UserContainerId = "UserSettings"
        });
        var database = (await _cosmosClient!.CreateDatabaseIfNotExistsAsync(cosmosOptions.Value.DatabaseId, cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token)).Database;
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = cosmosOptions.Value.UserContainerId,
                PartitionKeyPath = "/id"
            },
            cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        _repository = new UserSettingRepository(
            _cosmosClient!,
            cosmosOptions,
            Mock.Of<ILogger<UserSettingRepository>>());
    }

    [After(Class)]
    public static async Task ClassTeardown()
    {
        await _cosmosContainer.StopAsync();
        await _cosmosContainer.DisposeAsync();
        _cosmosClient?.Dispose();
    }

    [Test]
    public async Task ReturnsNull_WhenUserHasNoSettings()
    {
        var userId = UserId.Parse(Guid.NewGuid().ToString());
        var settings = await _repository.GetByUserIdAsync(userId);
        await Assert.That(settings).IsNull();
    }

    [Test]
    public async Task CreatesSettings_WhenUpserting()
    {
        var settings = new UserSettings
        {
            UserId = UserId.New(),
            CentsPerKilometer = 30,
            CentsPerHour = 500
        };

        await _repository.UpsertAsync(settings);
        var retrieved = await _repository.GetByUserIdAsync(settings.UserId);

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.UserId).IsEqualTo(settings.UserId);
        await Assert.That(retrieved.CentsPerKilometer).IsEqualTo((uint)30);
        await Assert.That(retrieved.CentsPerHour).IsEqualTo((uint)500);
    }

    [Test]
    public async Task UpdatesSettings_WhenUpsertingExisting()
    {
        // Arrange
        var settings = new UserSettings
        {
            UserId = UserId.New(),
            CentsPerKilometer = 30,
            CentsPerHour = 500
        };

        await _repository.UpsertAsync(settings);

        var newSettings = new UserSettings
        {
            UserId = settings.UserId,
            CentsPerKilometer = 35,
            CentsPerHour = 550
        };

        // Act
        await _repository.UpsertAsync(newSettings);

        // Assert
        var retrieved = await _repository.GetByUserIdAsync(settings.UserId);

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.UserId).IsEqualTo(settings.UserId);
        await Assert.That(retrieved.CentsPerKilometer).IsEqualTo((uint)35);
        await Assert.That(retrieved.CentsPerHour).IsEqualTo((uint)550);
    }
}
