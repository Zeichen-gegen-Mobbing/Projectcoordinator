data "azurerm_resource_group" "this" {
  name = "rg-ProjectCoordinator-${var.environment}"
}

data "azuread_client_config" "current" {
}

locals {
  redirect_path               = "/authentication/login-callback"
  client_preview_domain_left  = split(".", azurerm_static_web_app.this.default_host_name)[0]
  client_preview_domain_right = substr(azurerm_static_web_app.this.default_host_name, length(local.client_preview_domain_left) + 1, -1)
  client_preview_domains      = [for num in range(var.redirect_uris_number, var.redirect_uris_number + 100) : "https://${local.client_preview_domain_left}-${num}.${azurerm_static_web_app.this.location}.${local.client_preview_domain_right}${local.redirect_path}"]
}

module "application_registrations" {
  source      = "./modules/application_registrations"
  environment = var.environment
  redirect_uris = concat(
    ["https://${azurerm_static_web_app.this.default_host_name}${local.redirect_path}"],
    var.redirect_uris,
    local.client_preview_domains,
    var.custom_domain != null ? "https://${var.custom_domain}${local.redirect_path}" : []
  )
  permission_grant = false
}

resource "azurerm_cosmosdb_sql_container" "places" {
  account_name        = var.cosmos_account_name
  resource_group_name = var.cosmos_resource_group_name
  database_name       = var.cosmos_database_name
  name                = "Projectcoordinator-Places"
  partition_key_paths = ["/userId"]
}

resource "azurerm_application_insights" "this" {
  name                = "appi-ProjectCoordinator-${var.environment}"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = "westeurope"
  application_type    = "web"
  workspace_id        = data.azurerm_key_vault_secret.log_analytics_workspace_id.value
}
