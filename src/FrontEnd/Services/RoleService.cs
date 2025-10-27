using System.Net.Http.Json;

namespace FrontEnd.Services
{
    public class RoleService : IRoleService
    {
        private readonly HttpClient _httpClient;

        public RoleService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string[]> GetUserRolesAsync()
        {
            var roles = await _httpClient.GetFromJsonAsync<string[]>("user/roles");
            return roles ?? Array.Empty<string>();
        }
    }
}
