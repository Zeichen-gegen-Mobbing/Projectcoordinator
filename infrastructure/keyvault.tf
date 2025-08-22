data "azurerm_key_vault" "this" {
  name                = var.key_vault_name
  resource_group_name = data.azurerm_resource_group.this.name
}

data "azurerm_key_vault_secret" "cosmos_connection_string" {
  name         = "cosmos-connection-string"
  key_vault_id = data.azurerm_key_vault.this.id
}

data "azurerm_key_vault_secret" "loganalytics_workspace_id" {
  name         = "loganalytics-workspace-id"
  key_vault_id = data.azurerm_key_vault.this.id
}
