<#
.SYNOPSIS
Starts the Azure Cosmos DB emulator.
.NOTES
The pwsh version is required to fail on non zero exit codes from docker.
#>

#Requires -Version 7.4.0
#Requires -Modules CosmosDB

[CmdletBinding()]
param (
    # Number of times to try to reach the started Cosmos Emulator
    [int]$RetryCount = 15
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true # might be true by default

$image = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest"

$parameters = @(
    "--publish", "8081:8081"
    "--publish", "10250-10255:10250-10255"
    "--name", "linux-emulator"
    "--env", "AZURE_COSMOS_EMULATOR_PARTITION_COUNT=2"
    "--env", "AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1"
    "--detach",
    "--rm"
)
$containerId = docker ps --filter "name=linux-emulator" --format "{{.ID}}"
if ($containerId) {
    Write-Output "Cosmos Emulator already Running with container id $containerId"
}
else {
    # We need to pull the image as the evaluation period expires 180 days after publishing...
    docker pull $image
    $containerId = docker run @parameters $image

    Write-Output "Cosmos Emulator container started with container id $containerId"
}

[string[]]$logs = docker logs $containerId
for ($i = $RetryCount; $i -ge 0; $i--) {  
    if ($logs -and $logs[-1] -eq "Started") {
        break
    }
    Write-Progress -Activity "Waiting for emulator to start..." -Status "Attempt $($RetryCount - $i + 1)" -PercentComplete ((($RetryCount - $i) / $RetryCount) * 100)
    Start-Sleep -Seconds $i
    $logs = docker logs $containerId
}

if ($logs -and $logs[-1] -ne "Started") {
    throw "Emulator failed to start. Last log line: $($logs[-1])"
}

$parameters = @{
    Uri                  = 'https://localhost:8081/_explorer/emulator.pem'
    Method               = 'GET'
    OutFile              = 'emulatorcert.crt'
    SkipCertificateCheck = $True
}

$available = $false
for ($i = $RetryCount; $i -ge 0; $i--) {
    try {
        Invoke-WebRequest @parameters
        $available = $true
        break
    }
    catch {
        Write-Progress -Activity "Downloading emulator certificate" -Status $_.Exception.Message -PercentComplete ((($RetryCount - $i) / $RetryCount) * 100)
        Start-Sleep -Seconds $i
    }
}

if ($available) {
    Write-Output "You can open https://localhost:8081/_explorer/index.html . You may need to import the emulator certificate $($parameters.OutFile) into your trusted root certificate store. You may need to create the Container."
}
else {
    Write-Error -Message "Failed to download emulator certificate after $RetryCount attempts."
}

#region CosmosDB
$cosmosDbId = "cosql-shared-free-zgm"
$cosmosContainerId = "Projectcoordinator-Places"
$cosmosDbContext = New-CosmosDbContext -Emulator
try {
    $null = Get-CosmosDbDatabase -Context $cosmosDbContext -Id $cosmosDbId
    Write-Output "Database '$cosmosDbId' already exists."
}
catch {
    Write-Output "Creating database '$cosmosDbId'."
    $null = New-CosmosDbDatabase -Context $cosmosDbContext -Id $cosmosDbId
}

$cosmosDbContext = New-CosmosDbContext -Emulator -Database $cosmosDbId

try {
    $null = Get-CosmosDbCollection -Context $cosmosDbContext -Id $cosmosContainerId
    Write-Output "Collection '$cosmosContainerId' already exists."
}
catch {
    Write-Output "Creating collection '$cosmosContainerId' with 4000 RU/s throughput."
    $null = New-CosmosDbCollection -Context $cosmosDbContext -Id $cosmosContainerId -PartitionKey userId -OfferThroughput 4000
}

$ResponseHeader = $null
$documents = Get-CosmosDbDocument -Context $cosmosDbContext -CollectionId $cosmosContainerId -MaxItemCount 1 -ResponseHeader ([ref] $ResponseHeader)
if (-not $documents) {
    $userId = $([Guid]::NewGuid().ToString())
    
    # Place 1: Without TransportMode (defaults to Car)
    $document = @{
        id        = $([Guid]::NewGuid().ToString())
        userId    = $userId
        name      = "Berlin Office (Default)"
        latitude  = 52.5200
        longitude = 13.4050
    } | ConvertTo-Json
    $null = New-CosmosDbDocument -Context $cosmosDbContext -CollectionId $cosmosContainerId -DocumentBody $document -Encoding 'UTF-8' -PartitionKey $userId
    
    # Place 2: Explicit Car mode
    $document = @{
        id            = $([Guid]::NewGuid().ToString())
        userId        = $userId
        name          = "Berlin Office (Car)"
        latitude      = 52.5200
        longitude     = 13.4050
        transportMode = 0  # Car
    } | ConvertTo-Json
    $null = New-CosmosDbDocument -Context $cosmosDbContext -CollectionId $cosmosContainerId -DocumentBody $document -Encoding 'UTF-8' -PartitionKey $userId
    
    # Place 3: Train mode
    $document = @{
        id            = $([Guid]::NewGuid().ToString())
        userId        = $userId
        name          = "Berlin Office (Train)"
        latitude      = 52.5200
        longitude     = 13.4050
        transportMode = 1  # Train
    } | ConvertTo-Json
    $null = New-CosmosDbDocument -Context $cosmosDbContext -CollectionId $cosmosContainerId -DocumentBody $document -Encoding 'UTF-8' -PartitionKey $userId
    
    Write-Output "Created 3 test places for user $userId at Berlin coordinates with different transport modes."
}
#endregion