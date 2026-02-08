using Microsoft.Azure.Cosmos;

namespace api.Factories;

/// <summary>
/// Factory for creating CosmosClient instances.
/// Required to ensure the correct NamingPolicy is used for JSON serialization.
/// </summary>
public static class CosmosClientFactory
{
    public static CosmosClient CreateCosmosClient(string connectionString, CosmosClientOptions? options = null)
    {
        CosmosClientOptions newOptions = options ?? new CosmosClientOptions();
        newOptions.UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };
        return new CosmosClient(connectionString, newOptions);
    }
}