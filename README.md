# Projectcoordinator

App um die Arbeit der Projektkoordination zu erleichtern

## Architecture

The FrontEnd is a Blazor WASM Application to be run in Azure Static Website. The BackEnd is a C# Azure Functions implementation intended to run as a Managed Api inside of the Static Web App. The data will be persisted in CosmosDB. Using these srvices allows us to run this app for free.

### Authentication

As we can't use Bring your own function (requires SWA Standard), we need to secure the api via SWA integrated authentication or implement authentication ourself. As using the integrated authentication of SWA prevents us from using access tokens to get the users from Graph API, we implement authentication using MSAL ourself.

## Deployment

The deployment of the infrastructure uses OpenTofu (see `./infrastructure`). The deployment of the code uses GitHub Actions to deploy the infrastructure. These are using a remote backend in Azure and user managed identity to access resources and backend using OIDC.

## Configuration

Following configurations are required:

- `Cosmos__ConnectionString`
- `OpenRouteService__ApiKey`

## Local Development

The suggested Local Development Environment is using Visual Studio Code. To run Front- and Backend together use [static web app cli](https://learn.microsoft.com/en-us/azure/static-web-apps/local-development) `swa start`

### FrontEnd

The FrontEnd can be run without the backend thanks to the `FakeServices`. To enable the FakeServices you need to Change the `Program.cs`(src\FrontEnd\Program.cs) to load the FakeService Implementations eg.

```C#
builder.Services.AddScoped<IUserService,FakeUserService>();
builder.Services.AddScoped<IPlaceService, FakePlaceService>();
```

### BackEnd

To run the backend you need the Azure Function Core Tools. Furthermore you need the [Azure Cosmos Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator?tabs=docker-windows%2Ccsharp&pivots=api-nosql). You can run `pwsh ./scripts/Start-PcDatabase.ps1` if you have PowerShell and docker installed.

Configure the required Configurations in `local.settings.json`. Furthermore you may disable CORS for local development:

```json
"Host": {
    "CORS": "*"
}
```

#### Add Places

To add places for now, you can use the `ThunderClient` Extension for Visual Studio Code. Just `post` the following JSON to `http://localhost:7127/api/CreatePlace`

```json
{
  "UserId": "021d4e6b-c0bb-466c-86be-4143f1e7ed8a",
  "Name": "Woanders",
  "Longitude": 48.1411,
  "Latitude": 10.5751
}
```

## Azure Deployment

The Deployment to Azure is done using GitHub Actions and Open Tofu.

- Register App in Azure and add as Variables to Github Secrets
- TODO: Use <https://github.com/Azure-Samples/github-terraform-oidc-ci-cd/blob/main/terraform-example-deploy/main.tf> as baseline to create required config?

TODO: Configure TF-Backend & maybe encryption
TODO: Configure Functions/Cosmos DB
