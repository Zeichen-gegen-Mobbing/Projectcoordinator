using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using api.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Repositories
{
    internal sealed class PlaceRepository : IPlaceRepository
    {
        private readonly Task<Container> initContainer;
        private readonly ILogger<PlaceRepository> logger;

        public PlaceRepository(CosmosClient client, IOptions<CosmosSettings> options, ILogger<PlaceRepository> logger)
        {
            this.logger = logger;
            initContainer = Init(options.Value, client);
        }

        private async Task<Container> Init(CosmosSettings settings, CosmosClient client)
        {
            logger.LogDebug("Initializing Container");
            var container = client.GetContainer(settings.DatabaseId, settings.ContainerId);
            // verify that container exists
            await container.ReadContainerAsync();
            return container;
        }
        public async Task<PlaceEntity> AddAsync(PlaceEntity entity)
        {
            var container = await initContainer;
            return await container.CreateItemAsync(entity);
        }

        public async Task<IEnumerable<PlaceEntity>> GetAllAsync()
        {
            logger.LogDebug("Get All Places from CosmosDB");
            var container = await initContainer;
            var list = new List<PlaceEntity>();
            using FeedIterator<PlaceEntity> iterator = container.GetItemLinqQueryable<PlaceEntity>().ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                FeedResponse<PlaceEntity> response = await iterator.ReadNextAsync(default);

                foreach (PlaceEntity sampleObject in response)
                {
                    list.Add(sampleObject);
                }
            }
            return list;
        }
    }
}
