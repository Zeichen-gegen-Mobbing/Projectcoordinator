resource "github_repository_environment" "this" {
  environment = var.environment
  repository  = var.github_repository
}

resource "github_actions_environment_variable" "azure_client_id" {
  repository    = var.github_repository
  environment   = github_repository_environment.this.environment
  variable_name = "AZURE_CLIENT_ID"
  value         = azurerm_user_assigned_identity.this.client_id
}

resource "github_actions_environment_variable" "azure_subscription_id" {
  repository    = var.github_repository
  environment   = github_repository_environment.this.environment
  variable_name = "AZURE_SUBSCRIPTION_ID"
  value         = data.azurerm_client_config.current.subscription_id
}

resource "github_actions_environment_variable" "azure_tenant_id" {
  repository    = var.github_repository
  environment   = github_repository_environment.this.environment
  variable_name = "AZURE_TENANT_ID"
  value         = data.azurerm_client_config.current.tenant_id
}

resource "github_actions_environment_variable" "backend_azure_resource_group_name" {
  repository    = var.github_repository
  environment   = github_repository_environment.this.environment
  variable_name = "BACKEND_AZURE_RESOURCE_GROUP_NAME"
  value         = azurerm_resource_group.this.name
}

resource "github_actions_environment_variable" "backend_azure_storage_account_name" {
  repository    = var.github_repository
  environment   = github_repository_environment.this.environment
  variable_name = "BACKEND_AZURE_STORAGE_ACCOUNT_NAME"
  value         = azurerm_storage_account.this.name
}

resource "github_actions_environment_variable" "backend_azure_storage_account_container_name" {
  repository    = var.github_repository
  environment   = github_repository_environment.this.environment
  variable_name = "BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME"
  value         = azurerm_storage_container.this.name
}
