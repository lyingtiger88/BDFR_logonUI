[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$TaskName = "BDFR LogonUI Physical Lab Auto Rollback"

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script from an elevated PowerShell window."
}

$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($task) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "BDFR physical-lab auto rollback cancelled." -ForegroundColor Yellow
}
else {
    Write-Host "No BDFR auto-rollback task is currently registered."
}

Write-Host "The BDFR Credential Provider itself remains registered until Unregister-BDFRProvider.ps1 is run."
