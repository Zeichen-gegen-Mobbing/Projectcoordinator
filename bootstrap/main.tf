locals {
  default_audience_name = "api://AzureADTokenExchange"
  github_issuer_url     = "https://token.actions.githubusercontent.com"
}

data "azurerm_client_config" "current" {
}

moved {
  from = azurerm_resource_group.this
  to   = azurerm_resource_group.deployment
}

resource "azurerm_resource_group" "deployment" {
  name     = "rg-ProjectcoordinatorDeploy-${var.environment}"
  location = var.location
}

moved {
  from = azurerm_user_assigned_identity.this
  to   = azurerm_user_assigned_identity.deployment
}

resource "azurerm_user_assigned_identity" "deployment" {
  location            = var.location
  name                = "id-ProjectcoordinatorDeploy-${var.environment}"
  resource_group_name = azurerm_resource_group.deployment.name
}

moved {
  from = azurerm_federated_identity_credential.this
  to   = azurerm_federated_identity_credential.deployment
}

resource "azurerm_federated_identity_credential" "deployment" {
  name                = "${var.github_organization}-${var.github_repository}"
  resource_group_name = azurerm_resource_group.deployment.name
  audience            = [local.default_audience_name]
  issuer              = local.github_issuer_url
  parent_id           = azurerm_user_assigned_identity.deployment.id
  subject             = "repo:${var.github_organization}/${var.github_repository}:environment:${var.environment}"
}

moved {
  from = azurerm_storage_account.this
  to   = azurerm_storage_account.deployment
}

resource "azurerm_storage_account" "deployment" {
  name                     = "st${var.environment}tfprojectcoordinato"
  resource_group_name      = azurerm_resource_group.deployment.name
  location                 = azurerm_resource_group.deployment.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

moved {
  from = azurerm_storage_container.this
  to   = azurerm_storage_container.deployment
}

resource "azurerm_storage_container" "deployment" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.deployment.id
  container_access_type = "private"
}

resource "azurerm_role_assignment" "storage_container" {
  scope                = azurerm_storage_container.deployment.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = azurerm_user_assigned_identity.deployment.principal_id
}

data "azuread_application_published_app_ids" "well_known" {}

resource "azuread_service_principal" "msgraph" {
  client_id    = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph
  use_existing = true
}

resource "azuread_app_role_assignment" "graph_applications_owned" {
  app_role_id         = azuread_service_principal.msgraph.app_role_ids["Application.ReadWrite.OwnedBy"]
  principal_object_id = azurerm_user_assigned_identity.deployment.principal_id
  resource_object_id  = azuread_service_principal.msgraph.object_id
}

## Bootstrap Resource Group to allow access only to it

resource "azurerm_resource_group" "bootstrap" {
  name     = "rg-Projectcoordinator-${var.environment}"
  location = var.location
}

resource "azurerm_role_assignment" "resource_group" {
  scope                = azurerm_resource_group.bootstrap.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_user_assigned_identity.deployment.principal_id
}
