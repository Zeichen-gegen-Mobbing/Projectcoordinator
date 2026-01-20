using System.Net;
using System.Net.Http.Json;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class UserSettingsService(HttpClient httpClient) : IUserSettingsService
    {
        public async Task<UserSettings?> GetUserSettingsAsync(UserId userId)
        {
            var response = await httpClient.GetAsync($"/api/users/{userId.Value}/settings");
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserSettings>();
        }

        public async Task<UserSettings> UpsertUserSettingsAsync(UserId userId, UserSettings settings)
        {
            var response = await httpClient.PutAsJsonAsync($"/api/users/{userId.Value}/settings", settings);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<UserSettings>();
            if (result == null)
            {
                throw new InvalidOperationException("Failed to upsert user settings");
            }
            return result;
        }

        public async Task DeleteUserSettingsAsync(UserId userId)
        {
            var response = await httpClient.DeleteAsync($"/api/users/{userId.Value}/settings");
            response.EnsureSuccessStatusCode();
        }

        public async Task<UserSettings> GetDefaultSettingsAsync()
        {
            var response = await httpClient.GetAsync("/api/default-settings");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<UserSettings>();
            if (result == null)
            {
                throw new InvalidOperationException("Failed to get default settings");
            }
            return result;
        }

        public async Task<UserSettings> UpsertDefaultSettingsAsync(UserSettings settings)
        {
            var response = await httpClient.PutAsJsonAsync("/api/default-settings", settings);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<UserSettings>();
            if (result == null)
            {
                throw new InvalidOperationException("Failed to upsert default settings");
            }
            return result;
        }
    }
}
