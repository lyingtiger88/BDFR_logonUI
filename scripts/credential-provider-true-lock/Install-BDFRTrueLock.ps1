[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DllPath,

    [string]$ExperiencePath = "$env:ProgramFiles\BDFR\LogonUI\BDFR.LogonUI.Demo.exe",

    [ValidateRange(5, 120)]
    [int]$RollbackMinutes = 15,

    [Parameter(Mandatory = $true)]
    [string]$Confirmation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RequiredConfirmation = "I-UNDERSTAND-BDFR-TRUE-LOCK-TEST"
$ProviderClsid = "{55C142FF-F7C7-4A89-B6F1-1D64855CD366}"
$ProviderName = "BDFR LogonUI Secure Lock"
$TaskName = "BDFR LogonUI True Lock Auto Rollback"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell window."
    }
}

function Get-RegistryValueSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $exists = $false
    $value = $null
    $kind = $null

    if (Test-Path $Path) {
        $item = Get-Item -Path $Path
        try {
            $value = $item.GetValue($Name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            if ($null -ne $value) {
                $exists = $true
                $kind = $item.GetValueKind($Name).ToString()
            }
        }
        catch {}
    }

    [pscustomobject]@{
        Path = $Path
        Name = $Name
        Exists = $exists
        Value = $value
        Kind = $kind
    }
}

Assert-Administrator

if ($Confirmation -ne $RequiredConfirmation) {
    throw "Confirmation text is incorrect. True Lock installation was not performed."
}

if (-not [Environment]::Is64BitOperatingSystem) {
    throw "BDFR True Lock currently supports only 64-bit Windows."
}

$os = Get-CimInstance Win32_OperatingSystem
if ([Version]$os.Version -lt [Version]"10.0.17763") {
    throw "Windows 10 1809 or newer is required."
}

$resolvedDll = (Resolve-Path $DllPath).Path
if ([IO.Path]::GetFileName($resolvedDll) -ne "BDFR.LogonUI.CredentialProvider.dll") {
    throw "DllPath must point to BDFR.LogonUI.CredentialProvider.dll."
}

$resolvedExperience = (Resolve-Path $ExperiencePath).Path
if ([IO.Path]::GetFileName($resolvedExperience) -ne "BDFR.LogonUI.Demo.exe") {
    throw "ExperiencePath must point to BDFR.LogonUI.Demo.exe."
}

$credentialProvidersRoot = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers"
$providerKey = Join-Path $credentialProvidersRoot $ProviderClsid
$comKey = "HKLM:\SOFTWARE\Classes\CLSID\$ProviderClsid"
$inprocKey = Join-Path $comKey "InprocServer32"

if (Test-Path $providerKey) {
    throw "BDFR Credential Provider is already registered. Roll it back before installing True Lock again."
}

$existingProviders = @(Get-ChildItem $credentialProvidersRoot -ErrorAction Stop)
if ($existingProviders.Count -lt 2) {
    throw "Too few existing Windows credential providers were detected. Installation aborted."
}

$stateRoot = "$env:ProgramData\BDFR\LogonUI\CredentialProvider"
$installRoot = "$env:ProgramFiles\BDFR\LogonUI\CredentialProvider"
New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null

$policySystem = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System"
$policyPersonalization = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization"
$embeddedLogon = "HKLM:\SOFTWARE\Microsoft\Windows Embedded\EmbeddedLogon"
$bdfrSettings = "HKLM:\SOFTWARE\BDFR\LogonUI"

$registrySnapshots = @(
    Get-RegistryValueSnapshot -Path $policySystem -Name "DefaultCredentialProvider"
    Get-RegistryValueSnapshot -Path $policyPersonalization -Name "NoLockScreen"
    Get-RegistryValueSnapshot -Path $embeddedLogon -Name "BrandingNeutral"
    Get-RegistryValueSnapshot -Path $embeddedLogon -Name "AnimationDisabled"
    Get-RegistryValueSnapshot -Path $embeddedLogon -Name "UIVerbosityLevel"
    Get-RegistryValueSnapshot -Path $bdfrSettings -Name "TrueLockEnabled"
    Get-RegistryValueSnapshot -Path $bdfrSettings -Name "SecureExperiencePath"
)

$customLogonInitiallyEnabled = $null
$customLogonEnabledByInstaller = $false
$customLogonSupported = $false
$customLogonRestartNeeded = $false

try {
    $feature = Get-WindowsOptionalFeature -Online -FeatureName Client-EmbeddedLogon -ErrorAction Stop
    $customLogonSupported = $true
    $customLogonInitiallyEnabled = ($feature.State -eq "Enabled")

    if (-not $customLogonInitiallyEnabled) {
        $enableResult = Enable-WindowsOptionalFeature -Online -FeatureName Client-EmbeddedLogon -All -NoRestart -ErrorAction Stop
        $customLogonEnabledByInstaller = $true
        $customLogonRestartNeeded = [bool]$enableResult.RestartNeeded
    }
}
catch {
    $customLogonSupported = $false
}

$state = [pscustomobject]@{
    CreatedAt = (Get-Date).ToString("o")
    ProviderClsid = $ProviderClsid
    RegistryValues = $registrySnapshots
    CustomLogonSupported = $customLogonSupported
    CustomLogonFeatureInitiallyEnabled = $customLogonInitiallyEnabled
    CustomLogonFeatureWasEnabledByInstaller = $customLogonEnabledByInstaller
}

$statePath = Join-Path $stateRoot "true-lock-state.json"
$state | ConvertTo-Json -Depth 6 | Set-Content -Path $statePath -Encoding UTF8

$rollbackSource = Join-Path $PSScriptRoot "Rollback-BDFRTrueLock.ps1"
if (-not (Test-Path $rollbackSource)) {
    throw "Rollback-BDFRTrueLock.ps1 must be beside this installer script."
}

$rollbackTarget = Join-Path $stateRoot "Rollback-BDFRTrueLock.ps1"
Copy-Item $rollbackSource $rollbackTarget -Force

$powershell = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$rollbackArgs = "-NoProfile -ExecutionPolicy Bypass -File `"$rollbackTarget`" -StatePath `"$statePath`""
$action = New-ScheduledTaskAction -Execute $powershell -Argument $rollbackArgs
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes($RollbackMinutes)
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 5)

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -User "SYSTEM" -RunLevel Highest -Force | Out-Null

$installedDll = Join-Path $installRoot "BDFR.LogonUI.CredentialProvider.dll"
Copy-Item $resolvedDll $installedDll -Force

New-Item -Path $providerKey -Force | Out-Null
Set-Item -Path $providerKey -Value $ProviderName

New-Item -Path $comKey -Force | Out-Null
Set-Item -Path $comKey -Value "BDFR LogonUI Credential Provider"

New-Item -Path $inprocKey -Force | Out-Null
Set-Item -Path $inprocKey -Value $installedDll
New-ItemProperty -Path $inprocKey -Name "ThreadingModel" -PropertyType String -Value "Apartment" -Force | Out-Null

New-Item -Path $bdfrSettings -Force | Out-Null
New-ItemProperty -Path $bdfrSettings -Name "TrueLockEnabled" -PropertyType DWord -Value 1 -Force | Out-Null
New-ItemProperty -Path $bdfrSettings -Name "SecureExperiencePath" -PropertyType String -Value $resolvedExperience -Force | Out-Null

New-Item -Path $policySystem -Force | Out-Null
New-ItemProperty -Path $policySystem -Name "DefaultCredentialProvider" -PropertyType String -Value $ProviderClsid -Force | Out-Null

New-Item -Path $policyPersonalization -Force | Out-Null
New-ItemProperty -Path $policyPersonalization -Name "NoLockScreen" -PropertyType DWord -Value 1 -Force | Out-Null

if ($customLogonSupported) {
    New-Item -Path $embeddedLogon -Force | Out-Null
    New-ItemProperty -Path $embeddedLogon -Name "BrandingNeutral" -PropertyType DWord -Value 1 -Force | Out-Null
    New-ItemProperty -Path $embeddedLogon -Name "AnimationDisabled" -PropertyType DWord -Value 1 -Force | Out-Null
    New-ItemProperty -Path $embeddedLogon -Name "UIVerbosityLevel" -PropertyType DWord -Value 1 -Force | Out-Null
}

$rollbackAt = (Get-Date).AddMinutes($RollbackMinutes)

Write-Host ""
Write-Host "BDFR TRUE LOCK test mode installed." -ForegroundColor Green
Write-Host "Experience: $resolvedExperience"
Write-Host "Credential Provider: $installedDll"
Write-Host "BDFR is configured as the default credential provider."
Write-Host "Windows Password/PIN/Hello providers were NOT removed." -ForegroundColor Yellow
Write-Host "Auto rollback: $rollbackAt" -ForegroundColor Yellow
Write-Host "Custom Logon available: $customLogonSupported"
if ($customLogonRestartNeeded) {
    Write-Host "Custom Logon optional component requested a restart. Do NOT reboot for the first test." -ForegroundColor Yellow
}
Write-Host ""
Write-Host "FIRST TEST: press Win+L. BDFR should launch on the Windows secure desktop."
Write-Host "Unlock / Enter / Esc closes BDFR and reveals Windows authentication."
Write-Host "If anything behaves incorrectly, wait for auto rollback and then restart Windows."
