# GitHub Copilot Instructions for Projectcoordinator

## Repository Overview

**Purpose**: Projectcoordinator is a C# web application designed to facilitate project coordination work. It's built as a Blazor WebAssembly frontend with an Azure Functions API backend, using CosmosDB for data persistence. The entire solution is designed to run on Azure Static Web Apps for cost-effective deployment.

**Architecture**: 
- **Frontend**: Blazor WebAssembly (.NET 8.0) 
- **Backend**: Azure Functions v4 with .NET 8.0 isolated worker
- **Database**: Azure CosmosDB (with local emulator for development)
- **Deployment**: Azure Static Web Apps with GitHub Actions
- **Infrastructure**: OpenTofu/Terraform for Azure resource management

**Repository Size**: Medium-sized solution with 3 main projects, approximately 50+ files
**Languages**: C#, TypeScript/JavaScript, Terraform/HCL, PowerShell
**Target Runtime**: .NET 8.0

## Build and Development Requirements

### Prerequisites
- **.NET SDK 8.0 or later** (currently tested with .NET 9.0.302)
- **Azure Functions Core Tools v4** (version 4.0.7512 or later)
- **Static Web Apps CLI** (version 2.0.2 or later)
- **Docker** (for Cosmos DB emulator)
- **PowerShell 7.4.0+** (for database scripts)
- **OpenTofu** (for infrastructure management)

### Build Instructions

**Always follow this exact sequence for reliable builds:**

1. **Clean Build (recommended before any changes)**:
   ```powershell
   dotnet clean
   dotnet restore
   dotnet build
   ```
   - Build typically takes 15-50 seconds depending on cache
   - Always run `dotnet restore` after `dotnet clean` to avoid missing dependencies

2. **Full Solution Build**:
   ```powershell
   dotnet build Projectcoordinator.sln
   ```

3. **Test Execution** (currently no tests exist):
   ```powershell
   dotnet test
   ```

### Local Development Setup

**Database Setup (Required for API development)**:
1. **Always start the Cosmos emulator first**:
   ```powershell
   pwsh .\scripts\Start-PcDatabase.ps1
   ```
   - Script downloads and starts Azure Cosmos DB Linux emulator in Docker
   - Creates certificate file `emulatorcert.crt` for HTTPS connection
   - **Known Issue**: Script may fail with container errors - if this happens, manually stop existing containers and retry
   - Emulator accessible at: https://localhost:8081/_explorer/index.html

**Static Web Apps Development**:
1. **Using SWA CLI (Recommended)**:
   ```powershell
   swa start
   ```
   - Uses configuration from `swa-cli.config.json`
   - **Frontend runs on**: http://localhost:4280 (SWA proxy)
   - **Direct frontend**: http://localhost:5062
   - **API runs on**: http://localhost:7071
   - **Known Issue**: SWA CLI may fail to download Function Core Tools on Windows - ensure Azure Functions Core Tools is pre-installed

2. **Manual Development (Alternative)**:
   ```powershell
   # Terminal 1 - Frontend
   cd src/FrontEnd
   dotnet run --urls=http://localhost:5062
   
   # Terminal 2 - API (requires Cosmos emulator running)
   cd src/api
   func start
   ```

**Environment Configuration**:
- Frontend uses conditional compilation (`#if DEBUG`) to switch between fake services and real authentication
- API requires `local.settings.json` with Cosmos connection string and OpenRouteService API key
- CORS is disabled for local development in `local.settings.json`

### Infrastructure Management

**Prerequisites**: Authenticated with Azure CLI for backend state management

**Infrastructure Commands**:
```powershell
cd infrastructure
tofu init      # Initialize terraform (required first time)
tofu plan      # Preview changes
tofu apply     # Apply changes (requires approval)
```

**GitHub Actions Integration**:
- Infrastructure uses remote backend in Azure Storage
- Requires environment variables for authentication (OIDC)
- Variables needed: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`

## Project Structure and Architecture

### Solution Structure
```
Projectcoordinator.sln          # Main solution file (3 projects)
├── src/
│   ├── FrontEnd/               # Blazor WebAssembly project
│   │   ├── Components/         # Reusable Blazor components
│   │   ├── Layout/            # Layout components and CSS
│   │   ├── LocalAuthentication/ # Debug-only fake authentication
│   │   ├── Pages/             # Blazor page components
│   │   ├── Services/          # HTTP services and interfaces
│   │   └── wwwroot/           # Static web assets
│   ├── api/                   # Azure Functions project
│   │   ├── Entities/          # CosmosDB entity models
│   │   ├── Models/            # Request/response models
│   │   ├── Repositories/      # Data access layer
│   │   ├── Services/          # Business logic services
│   │   └── *.cs               # Function endpoints
│   └── Shared/                # Shared models between Frontend/API
├── infrastructure/            # OpenTofu/Terraform files
├── scripts/                   # PowerShell utility scripts
└── .github/workflows/         # GitHub Actions CI/CD
```

### Key Configuration Files
- `swa-cli.config.json` - Static Web Apps CLI configuration
- `src/api/local.settings.json` - Local Function App settings
- `src/api/host.json` - Azure Functions host configuration
- `infrastructure/*.tf` - Terraform infrastructure as code

### Authentication Architecture
- **Production**: Microsoft Authentication Library (MSAL) with Azure AD
- **Development**: Conditional compilation switches to fake authentication providers
- **API Security**: Secured via custom MSAL implementation

### Data Layer
- **Primary**: Azure CosmosDB with SQL API
- **Local Development**: Cosmos DB Linux emulator via Docker
- **Repository Pattern**: Implemented for data access abstraction
- **Connection**: Uses account key authentication (configured in app settings)

## Continuous Integration and Validation

### GitHub Actions Workflows
Located in `.github/workflows/`:

1. **`deploy.yml`** - Main deployment pipeline:
   - Triggers: Push to `main`, PRs to `main`
   - Builds and deploys to Azure Static Web Apps
   - Uses OpenTofu outputs for deployment configuration

2. **`analyze.yml`** - Code analysis:
   - Runs CodeQL for C# and JavaScript/TypeScript
   - Security vulnerability scanning
   - Scheduled weekly runs

3. **`infra_*.yml`** - Infrastructure management:
   - `infra_plan.yml` - Preview infrastructure changes on PRs
   - `infra_apply.yml` - Apply infrastructure changes on main branch
   - `infra_lint.yml` - Terraform/OpenTofu linting

### Code Quality Standards
- **C# Projects**: `TreatWarningsAsErrors=true` enabled
- **Exception**: Frontend project excludes `RZ10012` warning in Release builds
- **SonarLint**: Connected mode configured for "zeichen-gegen-mobbing" organization
- **No explicit linting rules**: No .editorconfig, stylecop, or custom rulesets found

### Validation Steps for Changes
**Before submitting PRs, always:**
1. Run `dotnet build` to ensure compilation
2. Test SWA CLI startup if modifying frontend configuration
3. Check that infrastructure plans apply cleanly if modifying Terraform

## Development Patterns and Conventions

### API Development
- **Function Naming**: Use descriptive names (e.g., `CreatePlace.cs`, `GetAllPlaces.cs`)
- **HTTP Routing**: Functions use attribute-based routing
- **Error Handling**: Custom `ProblemDetailsException` for consistent error responses
- **Dependency Injection**: Configured in `Program.cs` with options validation

### Frontend Development
- **Component Organization**: Separate folders for Components, Layout, Pages
- **Service Layer**: Interface-based services with fake implementations for testing
- **Styling**: Bootstrap CSS framework with custom CSS files
- **State Management**: Built-in Blazor component state management

### Configuration Management
- **Secrets**: Use Azure Key Vault for production (see `infrastructure/keyvault.tf`)
- **Local Development**: Store in `local.settings.json` (API) and `appsettings.json` (Frontend)
- **Environment Switching**: Conditional compilation for debug vs release builds

## Common Issues and Workarounds

### Build Issues
- **Issue**: NuGet restore failures after `dotnet clean`
- **Solution**: Always run `dotnet restore` explicitly after clean operations

### Local Development Issues
- **Issue**: Cosmos emulator container startup failures
- **Solution**: Stop all existing containers and restart the script
- **Issue**: SWA CLI fails to download Function Core Tools
- **Solution**: Pre-install Azure Functions Core Tools globally before using SWA CLI

### Infrastructure Issues
- **Issue**: Terraform state backend authentication
- **Solution**: Ensure Azure CLI is authenticated and has contributor access to backend storage account

## Trust These Instructions

These instructions are comprehensive and tested. Only search for additional information if:
1. The specific command or configuration mentioned here fails
2. You need to understand implementation details not covered in the high-level architecture
3. You encounter error messages not documented in the "Common Issues" section

Always start with the exact commands and procedures documented here before exploring alternatives.
