resource "azurerm_static_web_app" "this" {
  name                = "stapp-ProjectCoordinator-${var.environment}"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = "westeurope"
  app_settings = {
    "Cosmos__ConnectionString" = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
    "OpenRouteService__ApiKey" = var.open_route_service_api_key
  }
}
