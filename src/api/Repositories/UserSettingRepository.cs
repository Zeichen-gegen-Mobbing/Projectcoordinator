using api.Entities;
using api.Models;
using api.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Repositories;

internal sealed class UserSettingRepository : IUserSettingRepository
{
    private readonly Task<Container> initContainer;
    private readonly ILogger<UserSettingRepository> logger;

    public UserSettingRepository(CosmosClient client, IOptions<CosmosOptions> options, ILogger<UserSettingRepository> logger)
    {
        this.logger = logger;
        initContainer = Init(options.Value, client);
    }

    private async Task<Container> Init(CosmosOptions settings, CosmosClient client)
    {
        logger.LogDebug("Initializing Container");
        var container = client.GetContainer(settings.DatabaseId, settings.UsersContainerId);
        // verify that container exists
        await container.ReadContainerAsync();

        return container;
    }

    public async Task<UserSettings?> GetByUserIdAsync(ZgM.ProjectCoordinator.Shared.UserId userId)
    {
        logger.LogDebug("Get UserCostSettings for User {UserId} from CosmosDB", userId);
        var container = await initContainer;

        try
        {
            var response = await container.ReadItemAsync<UserSettingEntity>(
                userId.Value.ToString(),
                new PartitionKey(userId.Value.ToString()));

            return new UserSettings
            {
                UserId = response.Resource.Id,
                CentsPerKilometer = response.Resource.CentsPerKilometer,
                CentsPerHour = response.Resource.CentsPerHour
            };
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpsertAsync(UserSettings settings)
    {
        logger.LogDebug("Upsert UserCostSettings for User {UserId}", settings.UserId);
        var container = await initContainer;

        var entity = new UserSettingEntity
        {
            Id = settings.UserId,
            CentsPerKilometer = settings.CentsPerKilometer,
            CentsPerHour = settings.CentsPerHour
        };

        await container.UpsertItemAsync(entity);
    }

    public async Task DeleteAsync(ZgM.ProjectCoordinator.Shared.UserId userId)
    {
        logger.LogDebug("Delete UserCostSettings for User {UserId}", userId);
        var container = await initContainer;
        await container.DeleteItemAsync<UserSettingEntity>(
            userId.Value.ToString(),
            new PartitionKey(userId.Value.ToString()));
    }
}
