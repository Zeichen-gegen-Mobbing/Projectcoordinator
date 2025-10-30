using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FrontEnd.Services
{
    public class RoleService(HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider) : IRoleService
    {
        private Task<string[]>? _rolesTask;
        private string? _cachedUserId;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<bool> HasRole(string roleName)
        {
            var roles = await GetCurrentRolesAsync();
            var hasRole = roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

            if (!hasRole)
            {
                roles = await GetCurrentRolesAsync(forceReload: true);
                hasRole = roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
            }

            return hasRole;
        }

        private async Task<string[]> GetCurrentRolesAsync(bool forceReload = false)
        {
            var currentUserId = await GetCurrentUserIdAsync();

            await _semaphore.WaitAsync();
            try
            {
                if (!forceReload && _rolesTask != null && _cachedUserId == currentUserId)
                {
                    return await _rolesTask;
                }

                _cachedUserId = currentUserId;
                _rolesTask = httpClient.GetFromJsonAsync<string[]>("user/roles").ContinueWith(t => t.Result ?? []);

                return await _rolesTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string?> GetCurrentUserIdAsync()
        {
            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
