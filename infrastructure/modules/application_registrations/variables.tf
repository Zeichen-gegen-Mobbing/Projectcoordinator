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
