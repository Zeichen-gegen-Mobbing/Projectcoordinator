output "static_web_app_api_key" {
  value       = azurerm_static_web_app.this.api_key
  sensitive   = true
  description = "The API key for the Azure Static Web App. Used to deploy to in github actions"
}

output "permission_grant_required" {
  value       = "You need to grant admin consent to Client Application manually."
  description = "As doing this in Code would require Directory.Write, it is more reasonable to just do it manually"
}
