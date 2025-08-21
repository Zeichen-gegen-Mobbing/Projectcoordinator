data "azurerm_resource_group" "this" {
  name = "rg-ProjectCoordinator-${var.environment}"
}

data "azuread_client_config" "current" {
}

data "azuread_application_published_app_ids" "well_known" {}

data "azuread_service_principal" "microsoft_graph" {
  client_id = data.azuread_application_published_app_ids.well_known.result["MicrosoftGraph"]
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

