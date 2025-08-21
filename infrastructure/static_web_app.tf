resource "azurerm_static_web_app" "this" {
  name                = "stapp-ProjectCoordinator-${var.environment}"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = "westeurope"
  app_settings = {
    "Cosmos__ConnectionString" = var.cosmos_connection_string
    "Cosmos__DatabaseId"       = var.cosmos_database_name
    "Cosmos__ContainerId"      = azurerm_cosmosdb_sql_container.places.name
    "OpenRouteService__ApiKey" = var.open_route_service_api_key
  }
}
