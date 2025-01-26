resource "azurerm_resource_group" "this" {
  name     = "rg-ProjectCoordinator-${var.environment}"
  location = "westeurope"
}

data "azuread_client_config" "current" {
}

data "azuread_application_published_app_ids" "well_known" {}

resource "azuread_service_principal" "microsoft_graph" {
  client_id    = data.azuread_application_published_app_ids.well_known.result["MicrosoftGraph"]
  use_existing = true
}

resource "azuread_application" "client" {
  display_name     = "Projectcoordinator-${var.environment}"
  description      = "Application to login to the Projectcoordinator application"
  sign_in_audience = "AzureADMyOrg"
  owners           = [data.azuread_client_config.current.object_id]

  prevent_duplicate_names = true

  required_resource_access {
    resource_app_id = azuread_service_principal.microsoft_graph.client_id
    resource_access {
      id   = azuread_service_principal.microsoft_graph.oauth2_permission_scope_ids["User.ReadBasic.All"]
      type = "Scope"
    }
  }
  single_page_application {
    redirect_uris = concat(["https://${azurerm_static_web_app.this.default_host_name}/authentication/login-callback"], var.redirect_uris)
  }
}
