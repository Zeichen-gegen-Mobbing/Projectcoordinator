using FrontEnd;
#if DEBUG
using FrontEnd.LocalAuthentication;
using Microsoft.AspNetCore.Components.Authorization;
#endif
using FrontEnd.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;
using System.Net.Http.Json;
using ZgM.ProjectCoordinator.Shared;
using Microsoft.Authentication.WebAssembly.Msal.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load authentication configuration from API
using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var authConfig = await httpClient.GetFromJsonAsync<AuthenticationOptions>("api/authentication-config") ?? throw new InvalidOperationException("Failed to load authentication configuration from API");
builder.Services.AddSingleton(authConfig);

#if DEBUG
builder.Services.AddScoped<AuthenticationStateProvider, LocalAuthenticationProvider>();
builder.Services.AddRemoteAuthentication<RemoteAuthenticationState, RemoteUserAccount, MsalProviderOptions>();
#else
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    foreach (var scope in authConfig.ApiScopes)
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
    }
    options.ProviderOptions.Authentication.ClientId = authConfig.FrontEndClientId;
});
#endif

builder.Services.AddTransient<AuthorizationMessageHandler>();

var baseAddress = $"{builder.HostEnvironment.BaseAddress}api/";
builder.Services.AddHttpClient<ITripService, TripService>(client =>
{
    client.BaseAddress = new Uri(baseAddress);
})
.AddHttpMessageHandler(sp =>
{
    return sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler([baseAddress], [$"api://{authConfig.ApiClientId}/Trips.Calculate"]);
});

builder.Services.AddHttpClient<IRoleService, RoleService>(client =>
{
    client.BaseAddress = new Uri(baseAddress);
})
.AddHttpMessageHandler(sp =>
{
    return sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler([baseAddress]);
});

builder.Services.AddHttpClient<ILocationService, LocationService>(client =>
{
    client.BaseAddress = new Uri(baseAddress);
})
.AddHttpMessageHandler(sp =>
{
    return sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler([baseAddress], [$"api://{authConfig.ApiClientId}/Locations.Search"]);
});

#if DEBUG
builder.Services.AddScoped<IUserService, FakeUserService>();
#else
builder.Services.AddHttpClient<IUserService, GraphUserService>(client => 
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0"))
    .AddHttpMessageHandler<AuthorizationMessageHandler>(sp => {
        return sp.GetRequiredService<AuthorizationMessageHandler>()
            .ConfigureHandler(
                authorizedUrls: ["https://graph.microsoft.com/"],
                scopes: ["User.ReadBasic.All"]);
    });
#endif

await builder.Build().RunAsync();
