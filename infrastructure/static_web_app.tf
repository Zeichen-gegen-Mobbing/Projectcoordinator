resource "azurerm_static_web_app" "this" {
  name                = "stapp-ProjectCoordinator-${var.environment}"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = "westeurope"
  app_settings = {
    "Cosmos__ConnectionString"       = data.azurerm_key_vault_secret.cosmos_connection_string.value
    "Cosmos__DatabaseId"             = var.cosmos_database_name
    "Cosmos__ContainerId"            = azurerm_cosmosdb_sql_container.places.name
    "OpenRouteService__ApiKey"       = var.open_route_service_api_key
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.this.instrumentation_key
  }

  lifecycle {
    ignore_changes = [repository_url, repository_branch]
  }
}
