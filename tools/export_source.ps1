#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$command=Get-Command git -ErrorAction SilentlyContinue
$git=if ($command) {$command.Source} else {Join-Path $projectRoot '.local\git\cmd\git.exe'}
if (-not (Test-Path -LiteralPath $git)) {throw 'Git is required; install Git for Windows'}
$python=Join-Path $projectRoot '.venv\Scripts\python.exe'
if (-not (Test-Path -LiteralPath $python)) {$python='python'}
& $python (Join-Path $PSScriptRoot 'check_repository.py')
if ($LASTEXITCODE -ne 0) {throw 'Source index validation failed'}
$version=(Get-Content -LiteralPath (Join-Path $projectRoot 'VERSION') -Raw).Trim()
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$folder=Join-Path $projectRoot ('artifacts\source\GeekMagicMonitor-'+$version+'-'+$stamp)
if (Test-Path -LiteralPath $folder) {throw 'Export destination already exists'}
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$prefix=$folder.Replace('\','/')+'/'
& $git -C $projectRoot checkout-index --all "--prefix=$prefix"
if ($LASTEXITCODE -ne 0) {throw 'Git index export failed'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=$folder+'.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($folder,$archive,[IO.Compression.CompressionLevel]::Optimal,$true)
Write-Output "Source folder: $folder"
Write-Output "Source archive: $archive"
Get-FileHash -LiteralPath $archive -Algorithm SHA256
