terraform {
  required_providers {
    azuread = {
      source  = "hashicorp/azuread"
      version = "3.0.2"
    }
  }
}

provider "azuread" {
}
