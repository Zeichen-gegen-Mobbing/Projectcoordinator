using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class PlaceService(HttpClient httpClient, ILogger<PlaceService> logger, AuthenticationStateProvider authStateProvider) : IPlaceService
    {
        public async Task<IEnumerable<Place>> GetAllPlacesAsync()
        {
            using (logger.BeginScope(nameof(GetAllPlacesAsync)))
            {
                try
                {
                    var authState = await authStateProvider.GetAuthenticationStateAsync();
                    var userId = GetUserId(authState.User);

                    var result = await httpClient.GetFromJsonAsync<IEnumerable<Place>>($"users/{userId.Value}/places");
                    if (result is null)
                    {
                        throw new InvalidOperationException("Received null from API");
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get places");
                    throw;
                }
            }
        }

        public async Task<Place> CreatePlaceAsync(string name, double latitude, double longitude)
        {
            using (logger.BeginScope(nameof(CreatePlaceAsync)))
            {
                try
                {
                    var authState = await authStateProvider.GetAuthenticationStateAsync();
                    var userId = GetUserId(authState.User);

                    var placeRequest = new
                    {
                        UserId = userId.Value,
                        Name = name,
                        Latitude = latitude,
                        Longitude = longitude
                    };

                    var response = await httpClient.PostAsJsonAsync($"users/{userId.Value}/places", placeRequest);
                    response.EnsureSuccessStatusCode();

                    return new Place
                    {
                        Id = new PlaceId(Guid.NewGuid().ToString()),
                        UserId = userId,
                        Name = name
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create place");
                    throw;
                }
            }
        }

        public async Task<Place> UpdatePlaceAsync(PlaceId placeId, string name, double latitude, double longitude)
        {
            using (logger.BeginScope(nameof(UpdatePlaceAsync)))
            {
                try
                {
                    var authState = await authStateProvider.GetAuthenticationStateAsync();
                    var userId = GetUserId(authState.User);

                    var placeRequest = new
                    {
                        UserId = userId.Value,
                        Name = name,
                        Latitude = latitude,
                        Longitude = longitude
                    };

                    var response = await httpClient.PutAsJsonAsync($"users/{userId.Value}/places/{placeId.Value}", placeRequest);
                    response.EnsureSuccessStatusCode();

                    var result = await response.Content.ReadFromJsonAsync<Place?>();
                    if (!result.HasValue)
                    {
                        throw new InvalidOperationException("Received null from API");
                    }
                    return result.Value;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to update place {PlaceId}", placeId);
                    throw;
                }
            }
        }

        public async Task DeletePlaceAsync(PlaceId placeId)
        {
            using (logger.BeginScope(nameof(DeletePlaceAsync)))
            {
                try
                {
                    var authState = await authStateProvider.GetAuthenticationStateAsync();
                    var userId = GetUserId(authState.User);

                    var response = await httpClient.DeleteAsync($"users/{userId.Value}/places/{placeId.Value}");
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete place {PlaceId}", placeId);
                    throw;
                }
            }
        }

        private UserId GetUserId(ClaimsPrincipal user)
        {
            var oidClaim = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")
                ?? user.FindFirst(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub");

            if (oidClaim?.Value != null && Guid.TryParse(oidClaim.Value, out var guid))
            {
                return new UserId(guid);
            }

            throw new InvalidOperationException("User ID not found in claims");
        }
    }
}
