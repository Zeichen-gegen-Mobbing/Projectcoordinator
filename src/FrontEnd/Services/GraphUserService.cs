using System.Net.Http.Json;
using FrontEnd.Models;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class GraphUserService(IHttpClientFactory httpClientFactory, ILogger<GraphUserService> logger) : IUserService
    {
        public async Task<User> GetUserAsync(UserId Id)
        {
            using (logger.BeginScope(nameof(GetUserAsync)))
            {
                var httpClient = httpClientFactory.CreateClient("GraphAPI");

                var requestUri = $"users/{Id.Value}?$select=displayName,id";

                var graphUser = await httpClient.GetFromJsonAsync<GraphUser>(requestUri) ?? throw new InvalidOperationException($"Failed to retrieve user information for user ID: {Id}");
                return graphUser.DisplayName != null
                    ? new User(Id, graphUser.DisplayName)
                    : new User(Id);
            }
        }
    }
}
