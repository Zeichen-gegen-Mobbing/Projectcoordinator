data "azurerm_resource_group" "this" {
  name = "rg-ProjectCoordinator-${var.environment}"
}

data "azuread_client_config" "current" {
}

data "azuread_application_published_app_ids" "well_known" {}

data "azuread_service_principal" "microsoft_graph" {
  client_id = data.azuread_application_published_app_ids.well_known.result["MicrosoftGraph"]
}

locals {
  client_preview_domain_left  = split(".", azurerm_static_web_app.this.default_host_name)[0]
  client_preview_domain_right = substr(azurerm_static_web_app.this.default_host_name, length(local.client_preview_domain_left), -1)
  client_preview_domains      = [for num in range(var.redirect_uris_number, var.redirect_uris_number + 100) : "${local.client_preview_domain_left}-${num}.${local.client_preview_domain_right}"]
}
resource "azuread_application" "client" {
  display_name     = "Projectcoordinator-${var.environment}"
  description      = "Application to login to the Projectcoordinator application"
  sign_in_audience = "AzureADMyOrg"
  owners           = [data.azuread_client_config.current.object_id]

  prevent_duplicate_names = true

  required_resource_access {
    resource_app_id = data.azuread_service_principal.microsoft_graph.client_id
    resource_access {
      id   = data.azuread_service_principal.microsoft_graph.oauth2_permission_scope_ids["User.ReadBasic.All"]
      type = "Scope"
    }
  }
  single_page_application {
    redirect_uris = concat(["https://${azurerm_static_web_app.this.default_host_name}/authentication/login-callback"], var.redirect_uris)
  }
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
