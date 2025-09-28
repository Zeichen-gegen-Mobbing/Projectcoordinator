<#
.SYNOPSIS
Starts the Azure Cosmos DB emulator.
.NOTES
The pwsh version is required to fail on non zero exit codes from docker.
#>

#Requires -Version 7.4.0
[CmdletBinding()]
param (
    # Number of times to try to reach the started Cosmos Emulator
    [int]$RetryCount = 10
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
$tries = 0
while (!$logs -or $logs[-1] -ne "Started") {
    Start-Sleep -Seconds 1
    $tries++
    Write-Progress -Activity "Waiting for emulator to start..." -Status "Attempt $tries"
    Write-Output "Waiting for emulator to start..."
    $logs = docker logs $containerId
}

$parameters = @{
    Uri                  = 'https://localhost:8081/_explorer/emulator.pem'
    Method               = 'GET'
    OutFile              = 'emulatorcert.crt'
    SkipCertificateCheck = $True
}

$available = $false
for ($i = 0; $i -lt $RetryCount; $i++) {
    try {
        Start-Sleep -Seconds 1
        Invoke-WebRequest @parameters
        $available = $true
        break
    }
    catch {
        Write-Progress -Activity "Downloading emulator certificate" -Status "Attempt $i of $RetryCount"
    }
}

if ($available) {
    Write-Output "You can open https://localhost:8081/_explorer/index.html . You may need to import the emulator certificate into your trusted root certificate store."
}
else {
    Write-Error -Message "Failed to download emulator certificate after $RetryCount attempts." -Exception $Error[0].Exception
}