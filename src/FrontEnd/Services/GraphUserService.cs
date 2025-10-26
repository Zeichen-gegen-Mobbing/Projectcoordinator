using System.Net.Http.Json;
using FrontEnd.Models;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class GraphUserService(IHttpClientFactory httpClientFactory, ILogger<GraphUserService> logger) : IUserService
    {
        public async Task<User> GetUserAsync(UserId id)
        {
            using (logger.BeginScope(nameof(GetUserAsync)))
            {
                var httpClient = httpClientFactory.CreateClient("GraphAPI");

                var requestUri = $"users/{id.Value}?$select=displayName,id";

                var graphUser = await httpClient.GetFromJsonAsync<GraphUser>(requestUri) ?? throw new InvalidOperationException($"Failed to retrieve user information for user ID: {id}");
                return graphUser.DisplayName != null
                    ? new User(id, graphUser.DisplayName)
                    : new User(id);
            }
        }
    }
}
