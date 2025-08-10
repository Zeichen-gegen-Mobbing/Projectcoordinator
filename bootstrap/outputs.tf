output "AZURE_CLIENT_ID" {
  value = azurerm_user_assigned_identity.deployment.client_id
}

output "AZURE_SUBSCRIPTION_ID" {
  value = data.azurerm_client_config.current.subscription_id
}

output "AZURE_TENANT_ID" {
  value = data.azurerm_client_config.current.tenant_id
}

output "BACKEND_AZURE_RESOURCE_GROUP_NAME" {
  value = azurerm_resource_group.deployment.name
}

output "BACKEND_AZURE_STORAGE_ACCOUNT_NAME" {
  value = azurerm_storage_account.deployment.name
}

output "BACKEND_AZURE_STORAGE_ACCOUNT_CONTAINER_NAME" {
  value = azurerm_storage_container.deployment.name
}
