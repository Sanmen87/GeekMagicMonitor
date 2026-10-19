#requires -Version 5.1
[CmdletBinding(DefaultParameterSetName='Review')]
param(
    [Parameter(ParameterSetName='Install',Mandatory=$true)][switch]$Install,
    [Parameter(ParameterSetName='Remove',Mandatory=$true)][switch]$Remove
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$userSid=$identity.User.Value
$taskName='GeekMagicMonitor-SensorAgent-'+$userSid
$productRoot=Join-Path $env:ProgramFiles 'GeekMagicMonitor'
$installRoot=Join-Path $env:ProgramFiles 'GeekMagicMonitor\SensorAgent'
$agentExe=Join-Path $installRoot 'GeekMagicMonitor.SensorAgent.exe'
$source=Join-Path $projectRoot 'artifacts\sensor-agent'
$elevated=([Security.Principal.WindowsPrincipal]$identity).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# Refuse junctions/symlinks at every existing component of the protected destination.
function Assert-ProtectedPath([string]$Path) {
    $checkedPath=[IO.Path]::GetFullPath($Path)
    $programRoot=[IO.Path]::GetFullPath($env:ProgramFiles).TrimEnd('\')+'\'
    if (-not $checkedPath.StartsWith($programRoot,[StringComparison]::OrdinalIgnoreCase)) {throw 'Destination must be inside Program Files'}
    $part=$checkedPath
    while ($part) {
        if (Test-Path -LiteralPath $part) {
            $item=Get-Item -LiteralPath $part -Force
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {throw "Reparse point refused: $part"}
        }
        $part=Split-Path -Parent $part
    }
}
Assert-ProtectedPath $installRoot
$existing=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existing) {
    $actions=@($existing.Actions)
    $existingUser=$existing.Principal.UserId
    if ($existingUser -like 'S-1-*') {$existingSid=$existingUser}
    else {$existingSid=(New-Object Security.Principal.NTAccount($existingUser)).Translate([Security.Principal.SecurityIdentifier]).Value}
    if ($actions.Count -ne 1 -or $actions[0].Execute -ne $agentExe -or $existingSid -ne $userSid) {
        throw 'Existing task differs from this collector; refusing to replace it'
    }
}
if ($Remove) {
    if (-not $elevated) {throw 'Removal requires PowerShell running as administrator'}
    if ($existing) {
        Stop-ScheduledTask -TaskName $taskName
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    }
    # Keep installed binaries for review; no recursive deletion of a system directory.
    Write-Output "Task removed. Collector files retained at: $installRoot"
    return
}
if (-not (Test-Path -LiteralPath (Join-Path $source 'GeekMagicMonitor.SensorAgent.exe'))) {throw 'Build the monitor first'}
$action=New-ScheduledTaskAction -Execute $agentExe -WorkingDirectory $installRoot
$trigger=New-ScheduledTaskTrigger -AtLogOn -User $userSid
$trigger.Delay='PT10S'
$principal=New-ScheduledTaskPrincipal -UserId $userSid -LogonType Interactive -RunLevel Highest
$settings=New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
$task=New-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'GeekMagicMonitor local CPU temperature collector; publishes a read-only snapshot, no network or commands.'
if (-not $Install) {
    $reviewDir=Join-Path $projectRoot '.local\reports\sensor-agent'
    New-Item -ItemType Directory -Force -Path $reviewDir | Out-Null
    $xmlPath=Join-Path $reviewDir 'scheduled-task.xml'
    Export-ScheduledTask -InputObject $task | Set-Content -LiteralPath $xmlPath -Encoding Unicode
    Write-Output "REVIEW ONLY: no task registered, no system files changed."
    Write-Output "User: $($identity.Name) ($userSid)"
    Write-Output "Task: $taskName; logon trigger, highest rights, no password stored."
    Write-Output "Destination: $installRoot"
    Write-Output "Task XML: $xmlPath"
    Get-ChildItem -LiteralPath $source -File | Get-FileHash -Algorithm SHA256 | Select-Object Hash,Path
    return
}
if (-not $elevated) {throw 'Installation requires PowerShell running as administrator'}
if ($existing) {Stop-ScheduledTask -TaskName $taskName; Start-Sleep -Seconds 1}
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
Assert-ProtectedPath $installRoot
# Protected ACL: ordinary users can read/run, only administrators/SYSTEM can write.
$acl=New-Object Security.AccessControl.DirectorySecurity
$acl.SetAccessRuleProtection($true,$false)
$acl.SetOwner((New-Object Security.Principal.SecurityIdentifier('S-1-5-32-544')))
foreach ($entry in @(@('S-1-5-18','FullControl'),@('S-1-5-32-544','FullControl'),@('S-1-5-32-545','ReadAndExecute'))) {
    $sid=New-Object Security.Principal.SecurityIdentifier($entry[0])
    $rule=New-Object Security.AccessControl.FileSystemAccessRule($sid,$entry[1],'ContainerInherit,ObjectInherit','None','Allow')
    $acl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $productRoot -AclObject $acl
Set-Acl -LiteralPath $installRoot -AclObject $acl
foreach ($file in Get-ChildItem -LiteralPath $source -File) {
    $destination=Join-Path $installRoot $file.Name
    Assert-ProtectedPath $destination
    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    # Remove any explicit ACL from an older install; inherit the protected directory ACL.
    $fileAcl=Get-Acl -LiteralPath $destination
    $fileAcl.SetAccessRuleProtection($false,$false)
    foreach ($rule in @($fileAcl.Access | Where-Object {-not $_.IsInherited})) {$fileAcl.RemoveAccessRuleSpecific($rule)}
    Set-Acl -LiteralPath $destination -AclObject $fileAcl
}
Register-ScheduledTask -TaskName $taskName -InputObject $task -Force | Out-Null
Start-ScheduledTask -TaskName $taskName
Write-Output "Installed and started: $taskName"
Write-Output "Snapshot: $(Join-Path $installRoot ('snapshot-'+$userSid+'.json'))"
