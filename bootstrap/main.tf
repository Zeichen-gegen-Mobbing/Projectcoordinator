locals {
  default_audience_name = "api://AzureADTokenExchange"
  github_issuer_url     = "https://token.actions.githubusercontent.com"
}

data "azurerm_client_config" "current" {
}

resource "azurerm_resource_group" "this" {
  name     = "rg-ProjectcoordinatorDeploy-${var.environment}"
  location = var.location
}

resource "azurerm_user_assigned_identity" "this" {
  location            = var.location
  name                = "id-ProjectcoordinatorDeploy-${var.environment}"
  resource_group_name = azurerm_resource_group.this.name
}

resource "azurerm_federated_identity_credential" "this" {
  name                = "${var.github_organization}-${var.github_repository}"
  resource_group_name = azurerm_resource_group.this.name
  audience            = [local.default_audience_name]
  issuer              = local.github_issuer_url
  parent_id           = azurerm_user_assigned_identity.this.id
  subject             = "repo:${var.github_organization}/${var.github_repository}:environment:${var.environment}"
}

resource "azurerm_storage_account" "this" {
  name                     = "st${var.environment}tfprojectcoordinato"
  resource_group_name      = azurerm_resource_group.this.name
  location                 = azurerm_resource_group.this.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "this" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}

resource "azurerm_role_assignment" "storage_container" {
  scope                = azurerm_storage_container.this.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

data "azuread_application_published_app_ids" "well_known" {}

resource "azuread_service_principal" "msgraph" {
  client_id    = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph
  use_existing = true
}

resource "azuread_app_role_assignment" "graph_applications_owned" {
  app_role_id         = azuread_service_principal.msgraph.app_role_ids["Application.ReadWrite.OwnedBy"]
  principal_object_id = azurerm_user_assigned_identity.this.principal_id
  resource_object_id  = azuread_service_principal.msgraph.object_id
}
