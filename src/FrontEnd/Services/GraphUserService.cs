using System.Net.Http.Json;
using FrontEnd.Models;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class GraphUserService(HttpClient httpClient, ILogger<GraphUserService> logger) : IUserService
    {
        public async Task<User> GetUserAsync(UserId Id)
        {
            using (logger.BeginScope(nameof(GetUserAsync)))
            {
                var requestUri = $"users/{Id.Value}?$select=displayName,id";

                var graphUser = await httpClient.GetFromJsonAsync<GraphUser>(requestUri) ?? throw new InvalidOperationException($"Failed to retrieve user information for user ID: {Id}");
                return graphUser.DisplayName != null
                    ? new User(Id, graphUser.DisplayName)
                    : new User(Id);
            }
        }

        public async Task<IEnumerable<GraphUser>> SearchUsersAsync(string query)
        {
            using (logger.BeginScope(nameof(SearchUsersAsync)))
            {
                // Encode the query according to Graph $search requirements:
                // - The whole clause is enclosed in double quotes
                // - If the clause contains double quotes or backslash, escape them with a backslash
                // - All other special characters must be URL encoded
                // We ignore that until we got problems
                var escaped = query.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var searchQuery = $"\"displayName:{escaped}\" OR \"mail:{escaped}\" OR \"givenName:{escaped}\" OR \"surname:{escaped}\"";
                var requestUri = $"users?$search={searchQuery}&$select=displayName,id,mail&$top=10";

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.Add("ConsistencyLevel", "eventual");

                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var searchResponse = await response.Content.ReadFromJsonAsync<GraphSearchResponse>();
                return searchResponse?.Value ?? Array.Empty<GraphUser>();
            }
        }

        private record GraphSearchResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public GraphUser[]? Value { get; init; }
        }
    }
}
