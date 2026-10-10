[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$TaskName = "BDFR LogonUI True Lock Auto Rollback"

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script from an elevated PowerShell window."
}

$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($task) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "BDFR True Lock automatic rollback was cancelled." -ForegroundColor Green
}
else {
    Write-Host "No BDFR True Lock auto-rollback task is currently registered."
}

Write-Host "True Lock remains enabled. Keep Windows built-in Password/PIN/Hello providers available." -ForegroundColor Yellow
