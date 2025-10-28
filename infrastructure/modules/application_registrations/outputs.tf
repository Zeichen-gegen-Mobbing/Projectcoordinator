output "api_client_id" {
  description = "The client_id of the Api App Registration"
  value       = azuread_application_registration.api.client_id
}
output "frontend_client_id" {
  description = "The client_id of the Frontend App Registration"
  value       = azuread_application_registration.client.client_id
}
output "api_scopes" {
  description = "The API scopes with full URIs"
  value       = [for scope in azuread_application_permission_scope.api_access : "api://${azuread_application_registration.api.client_id}/${scope.value}"]
}
