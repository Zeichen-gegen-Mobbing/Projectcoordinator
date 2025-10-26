terraform {
  required_providers {
    azuread = {
      source  = "hashicorp/azuread"
      version = ">= 3.0.2"
    }
    random = {
      source  = "hashicorp/random"
      version = ">= 3.6"
    }
  }
}

