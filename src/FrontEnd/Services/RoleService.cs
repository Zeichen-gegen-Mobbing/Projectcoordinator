using System.Net.Http.Json;

namespace FrontEnd.Services
{
    public class RoleService(HttpClient httpClient) : IRoleService
    {
        private string[]? _cachedRoles;
        private Task<string[]>? _loadingTask;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<bool> HasRole(string roleName)
        {
            var roles = await GetRolesAsync();
            return roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string[]> GetRolesAsync()
        {
            if (_cachedRoles != null)
            {
                return _cachedRoles;
            }

            await _semaphore.WaitAsync();
            try
            {
                if (_cachedRoles != null)
                {
                    return _cachedRoles;
                }

                if (_loadingTask != null)
                {
                    return await _loadingTask;
                }

                _loadingTask = LoadRolesAsync();
                _cachedRoles = await _loadingTask;
                return _cachedRoles;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string[]> LoadRolesAsync()
        {
            var roles = await httpClient.GetFromJsonAsync<string[]>("user/roles");
            return roles ?? [];
        }
    }
}
