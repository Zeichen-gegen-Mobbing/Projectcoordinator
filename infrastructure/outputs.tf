output "static_web_app_api_key" {
  value       = azurerm_static_web_app.this.api_key
  sensitive   = true
  description = "The API key for the Azure Static Web App. Used to deploy to in github actions"
}

output "client_id" {
  value       = azuread_application_registration.client.client_id
  description = "Client Id to enter manually into src\FrontEnd\wwwroot\appsettings.json"
}
