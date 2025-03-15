variable "location" {
  type        = string
  description = "The Azure region where the resources will be deployed."
  default     = "Germany West Central"
}

variable "github_personal_access_token" {
  type        = string
  description = "The personal access token used to authenticate with GitHub."
  sensitive   = true
}

variable "github_organization" {
  type        = string
  description = "The GitHub organization that owns the repositories."
  default     = "Zeichen-gegen-Mobbing"
}

variable "github_repository" {
  type        = string
  description = "The GitHub repository that contains the Terraform configuration."
  default     = "Projectcoordinator"

}

variable "environment" {
  type        = string
  description = "The environment that the resources will be deployed to."
  default     = "prd"
}
