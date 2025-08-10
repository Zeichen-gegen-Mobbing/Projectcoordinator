variable "environment" {
  type        = string
  description = "The environment for which the infrastructure should be created. Is used to name resources uniquely."
}

variable "redirect_uris" {
  type        = list(string)
  description = "Additional redirect Uris. Usually for local development."
  default     = []
}
