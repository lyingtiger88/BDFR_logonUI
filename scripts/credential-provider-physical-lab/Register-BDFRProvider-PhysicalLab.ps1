[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DllPath,

    [ValidateRange(5, 120)]
    [int]$RollbackMinutes = 15,

    [Parameter(Mandatory = $true)]
    [string]$Confirmation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RequiredConfirmation = "I-UNDERSTAND-THIS-IS-A-PHYSICAL-LOGON-TEST"
$ProviderClsid = "{55C142FF-F7C7-4A89-B6F1-1D64855CD366}"
$ProviderName = "BDFR LogonUI Physical Lab Preview"
$TaskName = "BDFR LogonUI Physical Lab Auto Rollback"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell window."
    }
}

function Test-LooksLikeVirtualMachine {
    try {
        $system = Get-CimInstance Win32_ComputerSystem
        $signature = "$($system.Manufacturer) $($system.Model)"
        return $signature -match "Virtual|VMware|VirtualBox|KVM|QEMU|Hyper-V"
    }
    catch { return $false }
}

Assert-Administrator

if ($Confirmation -ne $RequiredConfirmation) {
    throw "Confirmation text is incorrect. Physical-machine registration was not performed."
}

if (Test-LooksLikeVirtualMachine) {
    throw "This script is for a physical-lab machine. Use the VM registration script on virtual machines."
}

if (-not [Environment]::Is64BitOperatingSystem) {
    throw "Only 64-bit Windows is supported by this experimental package."
}

$os = Get-CimInstance Win32_OperatingSystem
if ([Version]$os.Version -lt [Version]"10.0.17763") {
    throw "Windows 10 1809 or newer is required."
}

$resolvedDll = (Resolve-Path $DllPath).Path
if ([IO.Path]::GetFileName($resolvedDll) -ne "BDFR.LogonUI.CredentialProvider.dll") {
    throw "DllPath must point to BDFR.LogonUI.CredentialProvider.dll."
}

$credentialProvidersRoot = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers"
$providerKey = Join-Path $credentialProvidersRoot $ProviderClsid
$comKey = "HKLM:\SOFTWARE\Classes\CLSID\$ProviderClsid"
$inprocKey = Join-Path $comKey "InprocServer32"

if (Test-Path $providerKey) {
    throw "BDFR Credential Provider is already registered. Remove it before repeating the test."
}

$existingProviders = @(Get-ChildItem $credentialProvidersRoot -ErrorAction Stop)
if ($existingProviders.Count -lt 2) {
    throw "Too few existing Windows credential providers were detected. Registration aborted."
}

$stateRoot = Join-Path $env:ProgramData "BDFR\LogonUI\CredentialProvider"
$installRoot = Join-Path $env:ProgramFiles "BDFR\LogonUI\CredentialProvider"
New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$providersBackup = Join-Path $stateRoot "credential-providers-$stamp.reg"
$classesBackup = Join-Path $stateRoot "classes-clsid-$stamp.reg"

& reg.exe export "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers" $providersBackup /y | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Failed to back up the Credential Providers registry branch." }

& reg.exe export "HKLM\SOFTWARE\Classes\CLSID" $classesBackup /y | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Failed to back up the COM CLSID registry branch." }

$recoverySource = Join-Path $PSScriptRoot "Recovery-Unregister-BDFRProvider.ps1"
if (-not (Test-Path $recoverySource)) {
    throw "Recovery-Unregister-BDFRProvider.ps1 must be beside this script."
}

$recoveryTarget = Join-Path $stateRoot "Recovery-Unregister-BDFRProvider.ps1"
Copy-Item $recoverySource $recoveryTarget -Force

$powershell = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$action = New-ScheduledTaskAction -Execute $powershell -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$recoveryTarget`""
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes($RollbackMinutes)
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 5)

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -User "SYSTEM" `
    -RunLevel Highest `
    -Force | Out-Null

$installedDll = Join-Path $installRoot "BDFR.LogonUI.CredentialProvider.dll"
Copy-Item $resolvedDll $installedDll -Force

New-Item -Path $providerKey -Force | Out-Null
Set-Item -Path $providerKey -Value $ProviderName

New-Item -Path $comKey -Force | Out-Null
Set-Item -Path $comKey -Value "BDFR LogonUI Credential Provider"

New-Item -Path $inprocKey -Force | Out-Null
Set-Item -Path $inprocKey -Value $installedDll
New-ItemProperty -Path $inprocKey -Name "ThreadingModel" -PropertyType String -Value "Apartment" -Force | Out-Null

$rollbackAt = (Get-Date).AddMinutes($RollbackMinutes)

Write-Host ""
Write-Host "BDFR physical-lab Credential Provider registered." -ForegroundColor Green
Write-Host "DLL: $installedDll"
Write-Host "Registry backup: $providersBackup"
Write-Host "COM backup: $classesBackup"
Write-Host "AUTO ROLLBACK: $rollbackAt" -ForegroundColor Yellow
Write-Host ""
Write-Host "Do not disable Password, PIN, Windows Hello, or other Microsoft providers." -ForegroundColor Yellow
Write-Host "Use SIGN OUT for the first test. Do not reboot first."
Write-Host "This milestone is preview-only and cannot authenticate."
Write-Host "If the screen behaves incorrectly, wait for auto rollback, then restart Windows."
Write-Host "After a successful return to desktop, run Cancel-BDFRAutoRollback.ps1 only if you intentionally want to keep the preview registered for further testing."
