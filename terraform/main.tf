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
    redirect_uris = [
      "http://localhost:5062/authentication/login-callback",
      "http://localhost:62714/authentication/login-callback",
      "https://localhost:44313/authentication/login-callback",
      "https://localhost:7112/authentication/login-callback",
    ]
  }
}
