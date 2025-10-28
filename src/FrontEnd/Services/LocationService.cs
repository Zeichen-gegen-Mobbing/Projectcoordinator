using System.Net.Http.Json;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class LocationService(HttpClient httpClient, ILogger<LocationService> logger) : ILocationService
    {
        public async Task<IEnumerable<LocationSearchResult>> SearchLocationsAsync(string query)
        {
            using (logger.BeginScope(nameof(SearchLocationsAsync)))
            {
                try
                {
                    var encodedQuery = Uri.EscapeDataString(query);
                    var requestUri = $"locations?text={encodedQuery}";

                    var result = await httpClient.GetFromJsonAsync<IEnumerable<LocationSearchResult>>(requestUri);
                    if (result is null)
                    {
                        throw new InvalidOperationException("Received null, that's very unexpected");
                    }
                    else
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(httpClient.BaseAddress?.ToString() ?? "Nothing", ex);
                }
            }
        }
    }
}
