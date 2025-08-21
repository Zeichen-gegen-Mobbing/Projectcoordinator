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

variable "cosmos_connection_string" {
  type        = string
  description = "Connection string for the Cosmos DB."
  sensitive   = true
}
