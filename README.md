# Projectcoordinator

App um die Arbeit der Projektkoordination zu erleichtern

## Architecture

The FrontEnd is a Blazor WASM Application to be run in Azure Static Website. The BackEnd is a C# Azure Functions implementation intended to run as a Managed Api inside of the Static Web App. The data will be persisted in CosmosDB. Using these services allows us to run this app for free.

### Authentication

As we can't use Bring your own function (requires SWA Standard), we need to secure the api via SWA integrated authentication or implement authentication ourself. As using the integrated authentication of SWA prevents us from using access tokens to get the users from Graph API, we implement authentication using MSAL ourself.

## Deployment

The deployment of the infrastructure uses OpenTofu (see `./infrastructure`). The deployment of the code uses GitHub Actions to deploy the infrastructure. These are using a remote backend in Azure and user managed identity to access resources and backend using OIDC.

### Preview Environments

On every PR the site is pushed to a preview environment. As this is a different URL, the application must contain the Redirect URI. To allow this the parameter `redirect_uris_number` configures from which number 100 PRs are added as redirect URIs. Update this number when you have almost reached 100.

## Local Development

The suggested Local Development Environment is using Visual Studio Code. To run Front- and Backend together use [static web app cli](https://learn.microsoft.com/en-us/azure/static-web-apps/local-development) `swa start`

To run the backend you need the Azure Function Core Tools. Furthermore you need the [Azure Cosmos Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator?tabs=docker-windows%2Ccsharp&pivots=api-nosql). You can run `pwsh ./scripts/Start-PcDatabase.ps1` if you have PowerShell and docker installed.

Replace the empty Configurations in `src\api\local.settings.json`.

This is enough to run the application in `Debug` as there the authentication and authorization is disabled using Fake services.

If you want to run the app with authN & AuthZ to test Graph or be more like the deployed version, you need to configure the auth Settings. Add to the `src\api\local.settings.json`

```
"AzureAd__ClientId": "<ApiClientId>",
"AzureAd__Instance": "https://login.microsoftonline.com/",
"AzureAd__TenantId": "<TenantId>",
"Authentication__FrontEndClientId": "<FrontEndClientId>",
"Authentication__ApiScope": "api://<ApiClientId>/API.Access"
```

If you need to create the app registrations run the following:

```pwsh
cd infrastructure/modules/application_registrations
echo 'provider "azuread" { }' > provider.tf
tofu init
tofu apply -var="environment=<devsomename>" -var='redirect_uris=["http://localhost:4280/authentication/login-callback"]'
```

After configured run the Task `Start SWA (with Backend) in Release`.

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
