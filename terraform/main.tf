resource "azuread_application_registration" "client" {
  display_name     = "Projectcoordinator-${var.environment}"
  description      = "Application to login to the Projectcoordinator application"
  sign_in_audience = "AzureADMyOrg"
}
