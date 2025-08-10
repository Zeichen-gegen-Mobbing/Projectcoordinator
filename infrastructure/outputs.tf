output "static_web_app_api_key" {
  value       = azurerm_static_web_app.this.api_key
  sensitive   = true
  description = "The API key for the Azure Static Web App. Used to deploy to in github actions"
}
