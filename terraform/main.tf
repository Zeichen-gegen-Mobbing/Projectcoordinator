data "azuread_client_config" "current" {
}

resource "azuread_application" "client" {
  display_name     = "Projectcoordinator-${var.environment}"
  description      = "Application to login to the Projectcoordinator application"
  sign_in_audience = "AzureADMyOrg"
  owners           = [data.azuread_client_config.current.object_id]
}
