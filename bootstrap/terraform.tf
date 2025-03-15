terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.20"
    }
    github = {
      source  = "integrations/github"
      version = "~> 6.5"
    }
  }
}

provider "github" {
  token = var.github_personal_access_token
  owner = var.github_organization
}

provider "azurerm" {
  features {
  }
}
