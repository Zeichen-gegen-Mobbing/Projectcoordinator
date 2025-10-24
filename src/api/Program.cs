using api;
using api.Repositories;
using api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddOptionsWithValidateOnStart<CosmosSettings>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("Cosmos").Bind(settings);
        }).ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<OpenRouteServiceOptions>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection(settings.Title).Bind(settings);
        }).ValidateDataAnnotations();

        SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler();
        // Customize this value based on desired DNS refresh timer
        socketsHttpHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(5);
        // Registering the Singleton SocketsHttpHandler lets you reuse it across any HttpClient in your application
        services.AddSingleton<SocketsHttpHandler>(socketsHttpHandler);

        // Use a Singleton instance of the CosmosClient
        services.AddSingleton<CosmosClient>(provider =>
        {
            CosmosClientOptions options = new CosmosClientOptions()
            {
                UseSystemTextJsonSerializerWithOptions = new System.Text.Json.JsonSerializerOptions()
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                }
            };
            var cosmosSettings = provider.GetRequiredService<IOptions<CosmosSettings>>();
            return new CosmosClient(cosmosSettings.Value.ConnectionString, options);
        });

        services.AddHttpClient();

        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IPlaceService, PlaceCosmosService>();
        services.AddScoped<ITripService, TripOpenRouteService>();
        services.AddProblemDetails();
#if DEBUG
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = "https://fake-authority.local";
            options.Audience = "debug-clientid";
            options.TokenValidationParameters.ValidateIssuer = false;
            options.TokenValidationParameters.ValidateIssuerSigningKey = false;
            options.TokenValidationParameters.SignatureValidator = delegate (string token, Microsoft.IdentityModel.Tokens.TokenValidationParameters parameters)
            {
                return new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
            };
        });
#else
        services.AddAuthentication(sharedOptions =>
         {
             sharedOptions.DefaultScheme = Microsoft.Identity.Web.Constants.Bearer;
             sharedOptions.DefaultChallengeScheme = Microsoft.Identity.Web.Constants.Bearer;
         })
         .AddMicrosoftIdentityWebApi(context.Configuration);
#endif

    })
    .Build();
await host.RunAsync();