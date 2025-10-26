resource "azuread_application_registration" "api" {
  display_name = "Projectcoordinator-API-${var.environment}"
  description  = "API application for Projectcoordinator backend"
}

resource "azuread_application_identifier_uri" "api" {
  application_id = azuread_application_registration.api.id
  identifier_uri = "api://${azuread_application_registration.api.client_id}"
}

resource "random_uuid" "api_scope_id" {
}


resource "azuread_application_permission_scope" "api_access" {
  application_id             = azuread_application_registration.api.id
  scope_id                   = random_uuid.api_scope_id.result
  value                      = "API.Access"
  admin_consent_description  = "Allows the application to access the Projectcoordinator API on behalf of the signed-in user"
  admin_consent_display_name = "Access Projectcoordinator API"
  user_consent_description   = "Allows the application to access the Projectcoordinator API on your behalf"
  user_consent_display_name  = "Access Projectcoordinator API"
  type                       = "User"
}

resource "azuread_service_principal" "api" {
  client_id = azuread_application_registration.api.client_id
  owners    = [data.azuread_client_config.current.object_id]
}
