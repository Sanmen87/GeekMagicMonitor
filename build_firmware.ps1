#requires -Version 5.1
[CmdletBinding()]
param([string]$PythonExecutable,[string]$OutputDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (-not $projectRoot) { $projectRoot = (Get-Location).Path }
$python = if ($PythonExecutable) { $PythonExecutable } else { Join-Path $projectRoot '.venv\Scripts\python.exe' }
if (-not (Test-Path -LiteralPath $python)) { throw 'Local Python environment missing. See docs/FIRMWARE.md.' }
if (-not $env:PLATFORMIO_CORE_DIR) { $env:PLATFORMIO_CORE_DIR = Join-Path $projectRoot '.platformio' }
$env:PLATFORMIO_SETTING_ENABLE_TELEMETRY = 'No'
$firmware = Join-Path $projectRoot 'firmware'
$artifacts = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $projectRoot 'artifacts\firmware' }
$config = Join-Path $firmware 'data\config.json'
if (-not (Test-Path -LiteralPath $config)) {
    Copy-Item -LiteralPath (Join-Path $firmware 'data\config-hellocubic.example') -Destination $config
    Write-Output 'Created local config.json with empty Wi-Fi credentials.'
}
$null = New-Item -ItemType Directory -Path $artifacts -Force
# Build targets only. No upload or serial-port commands are exposed.
& $python -m platformio run --project-dir $firmware --environment esp12e
if ($LASTEXITCODE -ne 0) { throw "Firmware compilation failed: $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $firmware '.pio\build\esp12e\firmware.bin') -Destination (Join-Path $artifacts 'firmware.bin') -Force
& $python -m platformio run --project-dir $firmware --environment esp12e --target buildfs
if ($LASTEXITCODE -ne 0) { throw "Filesystem build failed: $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $firmware '.pio\build\esp12e\littlefs.bin') -Destination (Join-Path $artifacts 'littlefs.bin') -Force
foreach ($name in @('LICENSE','THIRD_PARTY_NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $artifacts -Force }
Copy-Item -LiteralPath (Join-Path $projectRoot 'licenses') -Destination $artifacts -Recurse -Force
Get-FileHash -LiteralPath (Join-Path $artifacts 'firmware.bin'), (Join-Path $artifacts 'littlefs.bin') -Algorithm SHA256
