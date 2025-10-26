data "azuread_application_published_app_ids" "well_known" {}

data "azuread_service_principal" "microsoft_graph" {
  client_id = data.azuread_application_published_app_ids.well_known.result["MicrosoftGraph"]
}

resource "azuread_application_registration" "client" {
  display_name = "Projectcoordinator-Client-${var.environment}"
  description  = "Application to login to the Projectcoordinator application"
}

resource "azuread_application_api_access" "client_graph" {
  application_id = azuread_application_registration.client.id
  api_client_id  = data.azuread_service_principal.microsoft_graph.client_id
  scope_ids      = [data.azuread_service_principal.microsoft_graph.oauth2_permission_scope_ids["User.ReadBasic.All"]]
}

resource "azuread_service_principal_delegated_permission_grant" "client_graph" {
  service_principal_object_id          = azuread_service_principal.client.object_id
  resource_service_principal_object_id = data.azuread_service_principal.microsoft_graph.object_id

  claim_values = ["User.ReadBasic.All"]
}

resource "azuread_application_api_access" "client_api" {
  application_id = azuread_application_registration.client.id
  api_client_id  = azuread_application_registration.api.client_id
  scope_ids      = [azuread_application_permission_scope.api_access.scope_id]
}

resource "azuread_service_principal_delegated_permission_grant" "client_api" {
  service_principal_object_id          = azuread_service_principal.client.object_id
  resource_service_principal_object_id = azuread_service_principal.api.object_id

  claim_values = [azuread_application_permission_scope.api_access.value]
}

resource "azuread_application_redirect_uris" "client" {
  application_id = azuread_application_registration.client.id
  type           = "SPA"
  redirect_uris  = var.redirect_uris
}
resource "azuread_service_principal" "client" {
  client_id = azuread_application_registration.client.client_id
  owners    = [data.azuread_client_config.current.object_id]
}
