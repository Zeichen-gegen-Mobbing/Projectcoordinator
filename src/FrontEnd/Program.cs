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

#if DEBUG
builder.Services.AddScoped<AuthenticationStateProvider, LocalAuthenticationProvider>();
builder.Services.AddRemoteAuthentication<RemoteAuthenticationState, RemoteUserAccount, MsalProviderOptions>();
#else
// Load authentication configuration from API
using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var authConfig = await httpClient.GetFromJsonAsync<AuthenticationOptions>("api/authentication-config");

if (authConfig == null)
{
    throw new InvalidOperationException("Failed to load authentication configuration from API");
}

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

builder.Services.AddTransient<GraphAuthorizationMessageHandler>();
builder.Services.AddTransient<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient("GraphAPI",
        client => client.BaseAddress = new Uri(
            string.Join("/",
                builder.Configuration.GetSection("MicrosoftGraph")["BaseUrl"] ??
                    "https://graph.microsoft.com",
                builder.Configuration.GetSection("MicrosoftGraph")["Version"] ??
                    "v1.0",
                string.Empty)))
    .AddHttpMessageHandler<GraphAuthorizationMessageHandler>();

var baseAddress = $"{builder.HostEnvironment.BaseAddress}api/";
builder.Services.AddHttpClient<ITripService, TripService>(client =>
{
    client.BaseAddress = new Uri(baseAddress);
})
#if DEBUG
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<CustomAuthorizationMessageHandler>();
        handler.ConfigureHandler([baseAddress]);
        return handler;
    });
#else
.AddHttpMessageHandler(sp => {
    var handler = sp.GetRequiredService<CustomAuthorizationMessageHandler>();
    handler.ConfigureHandler([baseAddress], authConfig.ApiScopes);
    return handler;
});
#endif

builder.Services.AddHttpClient<IRoleService, RoleService>(client =>
{
    client.BaseAddress = new Uri(baseAddress);
})
#if DEBUG
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<CustomAuthorizationMessageHandler>();
        handler.ConfigureHandler([baseAddress]);
        return handler;
    });
#else
.AddHttpMessageHandler(sp => {
    var handler = sp.GetRequiredService<CustomAuthorizationMessageHandler>();
    handler.ConfigureHandler([baseAddress], authConfig.ApiScopes);
    return handler;
});
#endif

#if DEBUG
builder.Services.AddScoped<IUserService, FakeUserService>();
#else
builder.Services.AddScoped<IUserService, GraphUserService>();
#endif

await builder.Build().RunAsync();
