data "azurerm_resource_group" "this" {
  name = "rg-ProjectCoordinator-${var.environment}"
}

data "azuread_client_config" "current" {
}

locals {
  client_preview_domain_left  = split(".", azurerm_static_web_app.this.default_host_name)[0]
  client_preview_domain_right = substr(azurerm_static_web_app.this.default_host_name, length(local.client_preview_domain_left) + 1, -1)
  client_preview_domains      = [for num in range(var.redirect_uris_number, var.redirect_uris_number + 100) : "https://${local.client_preview_domain_left}-${num}.${azurerm_static_web_app.this.location}.${local.client_preview_domain_right}/authentication/login-callback"]
}

module "application_registrations" {
  source      = "./modules/application_registrations"
  environment = var.environment
  redirect_uris = concat(
    ["https://${azurerm_static_web_app.this.default_host_name}/authentication/login-callback"],
    var.redirect_uris,
    local.client_preview_domains
  )
}
moved {
  from = random_uuid.api_scope_id
  to   = module.application_registrations.random_uuid.api_scope_id
}

moved {
  from = azuread_application_registration.api
  to   = module.application_registrations.azuread_application_registration.api
}

moved {
  from = azuread_application_identifier_uri.api
  to   = module.application_registrations.azuread_application_identifier_uri.api
}
moved {
  from = azuread_application_permission_scope.api_access
  to   = module.application_registrations.azuread_application_permission_scope.api_access
}

moved {
  from = azuread_service_principal.api
  to   = module.application_registrations.azuread_service_principal.api
}
moved {
  from = azuread_application_registration.client
  to   = module.application_registrations.azuread_application_registration.client
}

moved {
  from = azuread_application_api_access.graph
  to   = module.application_registrations.azuread_application_api_access.client_graph
}

moved {
  from = azuread_application_api_access.api
  to   = module.application_registrations.azuread_application_api_access.client_api
}

moved {
  from = azuread_application_redirect_uris.static_site
  to   = module.application_registrations.azuread_application_redirect_uris.client
}

moved {
  from = azuread_service_principal.client
  to   = module.application_registrations.azuread_service_principal.client
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
