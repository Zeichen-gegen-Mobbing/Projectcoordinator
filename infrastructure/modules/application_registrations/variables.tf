variable "environment" {
  type        = string
  description = "The environment for which the application registrations should be created. Is used to name resources uniquely."
  nullable    = false
}

variable "redirect_uris" {
  type        = list(string)
  description = "Redirect Uris for the client application."
  nullable    = false
}

variable "permission_grant" {
  type        = bool
  description = "Whether to create permission grants for the client application. As this required high privileges only reasonable when running manually as Admin."
  default     = true
  nullable    = false
}
