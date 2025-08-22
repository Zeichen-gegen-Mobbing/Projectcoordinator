variable "environment" {
  type        = string
  description = "The environment for which the infrastructure should be created. Is used to name resources uniquely."
}

variable "redirect_uris" {
  type        = list(string)
  description = "Additional redirect Uris. Usually for local development."
  default     = []
}

variable "open_route_service_api_key" {
  type        = string
  description = "API key for the OpenRouteService."
  sensitive   = true
}

variable "cosmos_account_name" {
  type        = string
  description = "Name of the Cosmos DB account."
  default     = "cosmos-shared-free-zgm"
}

variable "cosmos_resource_group_name" {
  type        = string
  description = "Name of the resource group for the Cosmos DB."
  default     = "rg-shared-prd"
}

variable "cosmos_database_name" {
  type        = string
  description = "Name of the Cosmos DB database."
  default     = "cosql-shared-free-zgm"
}

variable "key_vault_name" {
  type        = string
  description = "Name of the Key Vault."
}
