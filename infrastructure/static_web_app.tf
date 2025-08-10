resource "azurerm_static_web_app" "this" {
  name                = "stapp-ProjectCoordinator-${var.environment}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
}
