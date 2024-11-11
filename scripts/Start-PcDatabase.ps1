<#
.SYNOPSIS
Starts the Azure Cosmos DB emulator.
.NOTES
The pwsh version is required to fail on non zero exit codes from docker.
#>

#Requires -Version 7.4.0
[CmdletBinding()]
param (
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true # might be true by default

$parameters = @(
    "--publish", "8081:8081"
    "--publish", "10250-10255:10250-10255"
    "--name", "linux-emulator"
    "--env", "AZURE_COSMOS_EMULATOR_PARTITION_COUNT=2"
    "--detach",
    "--rm"
)
$containerId = docker ps --filter "name=linux-emulator" --format "{{.ID}}"
if ($containerId) {
    Write-Output "Cosmos Emulator already Running with container id $containerId"
}
else {

    $containerId = docker run @parameters mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

    Write-Output "Cosmos Emulator container started with container id $containerId"
}

[string[]]$logs = docker logs $containerId
while (!$logs -or $logs[-1] -ne "Started") {
    Start-Sleep -Seconds 1
    Write-Output "Waiting for emulator to start..."
    $logs = docker logs $containerId
}

Start-Sleep -Seconds 1

$parameters = @{
    Uri                  = 'https://localhost:8081/_explorer/emulator.pem'
    Method               = 'GET'
    OutFile              = 'emulatorcert.crt'
    SkipCertificateCheck = $True
}
Invoke-WebRequest @parameters

Write-Output "You can open https://localhost:8081/_explorer/index.html . You may need to import the emulator certificate into your trusted root certificate store."
