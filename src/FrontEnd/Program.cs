using FrontEnd;
#if DEBUG
using FrontEnd.LocalAuthentication;
#endif
using FrontEnd.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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

builder.Services.AddScoped<IUserService,FakeUserService>();
builder.Services.AddScoped<IPlaceService, FakePlaceService>();

await builder.Build().RunAsync();
