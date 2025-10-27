using api.Options;

namespace api.Extensions
{
    public static class HttpClientOpenRouteServiceExtensions
    {
        public static HttpClient ConfigureForOpenRouteService(this HttpClient client, OpenRouteServiceOptions options)
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
