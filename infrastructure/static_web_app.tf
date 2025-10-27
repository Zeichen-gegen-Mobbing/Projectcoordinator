resource "azurerm_static_web_app" "this" {
  name                = "stapp-ProjectCoordinator-${var.environment}"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = "westeurope"
  app_settings = {
    "Cosmos__ConnectionString"              = data.azurerm_key_vault_secret.cosmos_connection_string.value
    "Cosmos__DatabaseId"                    = var.cosmos_database_name
    "Cosmos__ContainerId"                   = azurerm_cosmosdb_sql_container.places.name
    "OpenRouteService__ApiKey"              = var.open_route_service_api_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.this.connection_string
    "AzureAd__ClientId"                     = module.application_registrations.api_client_id
    "AzureAd__Instance"                     = "https://login.microsoftonline.com/"
    "AzureAd__TenantId"                     = data.azuread_client_config.current.tenant_id
    "Authentication__FrontEndClientId"      = module.application_registrations.frontend_client_id
    "Authentication__ApiScope"              = "api://${module.application_registrations.api_client_id}/API.Access"
  }

  lifecycle {
    ignore_changes = [repository_url, repository_branch]
  }
}

import {
  to = azurerm_static_web_app_custom_domain.this[0]
  id = "/subscriptions/36fd3843-f200-408f-a432-9bd04c5af9be/resourceGroups/rg-ProjectCoordinator-prd/providers/Microsoft.Web/staticSites/stapp-ProjectCoordinator-prd/customDomains/projectcoordinator.z-g-m.de"
}
resource "azurerm_static_web_app_custom_domain" "this" {
  count             = var.custom_domain != null ? 1 : 0
  static_web_app_id = azurerm_static_web_app.this.id
  domain_name       = var.custom_domain
  validation_type   = "cname-delegation"
}
