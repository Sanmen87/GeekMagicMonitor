#requires -Version 5.1
[CmdletBinding()]
param([string]$OutputDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
if (-not $projectRoot) { $projectRoot=(Get-Location).Path }
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $projectRoot 'artifacts\monitor' }
$dotnet=Join-Path $projectRoot '.dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet='dotnet' }
$env:DOTNET_CLI_HOME=Join-Path $projectRoot '.dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
if (-not $env:NUGET_PACKAGES) { $env:NUGET_PACKAGES=Join-Path $projectRoot '.nuget\packages' }
& $dotnet run --project (Join-Path $projectRoot 'monitor\GeekMagicMonitor.Tests') --configuration Release -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Monitor core checks failed' }
& $dotnet publish (Join-Path $projectRoot 'monitor\GeekMagicMonitor') --configuration Release --runtime win-x64 --self-contained false --output $OutputDirectory -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Monitor build failed' }
& $dotnet publish (Join-Path $projectRoot 'monitor\GeekMagicMonitor.SensorAgent') --configuration Release --runtime win-x64 --self-contained false --output (Join-Path $projectRoot 'artifacts\sensor-agent') -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Sensor collector build failed' }
foreach ($folder in @($OutputDirectory,(Join-Path $projectRoot 'artifacts\sensor-agent'))) {
    foreach ($name in @('LICENSE','THIRD_PARTY_NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $folder -Force }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'licenses') -Destination $folder -Recurse -Force
}
Write-Output "Monitor: $(Join-Path $OutputDirectory 'GeekMagicMonitor.exe')"
