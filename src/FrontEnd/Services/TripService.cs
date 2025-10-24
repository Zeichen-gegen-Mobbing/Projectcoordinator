using System.Globalization;
using System.Net.Http.Json;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Services
{
    public class TripService(HttpClient httpClient, ILogger<TripService> logger) : ITripService
    {
        public async Task<IEnumerable<Trip>> GetTripsAsync(double latitude, double longitude)
        {
            using (logger.BeginScope(nameof(GetTripsAsync)))
            {
                try
                {
                    var uri = new Uri(string.Format(new CultureInfo("en-US"), "https://ambitious-island-0f6399d03-48.westeurope.1.azurestaticapps.net/api/trips?latitude={0}&longitude={1}", latitude, longitude));
                    uri = new Uri(string.Format(new CultureInfo("en-US"), "https://ambitious-island-0f6399d03-48.westeurope.1.azurestaticapps.net/api/trips"));

                    var result = await httpClient.GetFromJsonAsync<IEnumerable<Trip>>(uri);
                    if (result is null)
                    {
                        throw new InvalidOperationException("Received null, thats very unexpected");
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
