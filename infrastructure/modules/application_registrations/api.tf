resource "azuread_application_registration" "api" {
  display_name = "Projectcoordinator-API-${var.environment}"
  description  = "API application for Projectcoordinator backend"
}

resource "azuread_application_identifier_uri" "api" {
  application_id = azuread_application_registration.api.id
  identifier_uri = "api://${azuread_application_registration.api.client_id}"
}

locals {
  api_scopes = {
    "Trips.Calculate" = {
      consent_display_name = "Calculate Trips"
      consent_description  = "Allows to calculate trips to all places of all users."
    }
    "Places.CreateOnBehalfOf" = {
      consent_display_name = "Create Places on Behalf of User"
      consent_description  = "Allows to create places on behalf of another user."
    }
    "Locations.Search" = {
      consent_display_name = "Search for Locations"
      consent_description  = "Allows to search for locations."
    }
  }
  api_roles = {
    "projectcoordination" = {
      display_name = "Project Coordination"
      description  = "Allows access to project coordination features including trip planning."
    }
  }
}
resource "random_uuid" "api_scope_id" {
  for_each = local.api_scopes
}

resource "azuread_application_permission_scope" "api_access" {
  for_each                   = local.api_scopes
  application_id             = azuread_application_registration.api.id
  scope_id                   = random_uuid.api_scope_id[each.key].result
  value                      = each.key
  admin_consent_description  = each.value.consent_description
  admin_consent_display_name = each.value.consent_display_name
  user_consent_description   = each.value.consent_description
  user_consent_display_name  = each.value.consent_display_name
  type                       = "User"
}

resource "random_uuid" "api_role_id" {
  for_each = local.api_roles
}

resource "azuread_application_app_role" "api" {
  for_each             = local.api_roles
  application_id       = azuread_application_registration.api.id
  role_id              = random_uuid.api_role_id[each.key].result
  value                = each.key
  display_name         = each.value.display_name
  description          = each.value.description
  allowed_member_types = ["User"]
}

resource "azuread_service_principal" "api" {
  client_id = azuread_application_registration.api.client_id
  owners    = [data.azuread_client_config.current.object_id]
}
