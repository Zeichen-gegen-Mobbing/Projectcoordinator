using FrontEnd;
#if DEBUG
using FrontEnd.LocalAuthentication;
#endif
using FrontEnd.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["API_Prefix"] ?? builder.HostEnvironment.BaseAddress) });

#if DEBUG
LocalAuthenticationProvider.AddLocalAuthentication(builder.Services);
#else
    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    });
#endif

builder.Services.AddTransient<GraphAuthorizationMessageHandler>();

builder.Services.AddHttpClient("GraphAPI",
        client => client.BaseAddress = new Uri(
            string.Join("/",
                builder.Configuration.GetSection("MicrosoftGraph")["BaseUrl"] ??
                    "https://graph.microsoft.com",
                builder.Configuration.GetSection("MicrosoftGraph")["Version"] ??
                    "v1.0",
                string.Empty)))
    .AddHttpMessageHandler<GraphAuthorizationMessageHandler>();


#if DEBUG
builder.Services.AddHttpClient<ITripService, TripService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:4280");
})
    .AddHttpMessageHandler(services =>
    {
        var handler = new FakeAuthorizationMessageHandler(services.GetRequiredService<IConfiguration>());
        return handler;
    });
#else
builder.Services.AddHttpClient<ITripService, TripService>().AddHttpMessageHandler<AuthorizationMessageHandler>();
#endif

builder.Services.AddScoped<IUserService, FakeUserService>();

await builder.Build().RunAsync();
