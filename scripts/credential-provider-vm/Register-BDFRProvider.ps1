[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DllPath,
    [switch]$AllowNonVm
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProviderClsid = "{55C142FF-F7C7-4A89-B6F1-1D64855CD366}"
$ProviderName = "BDFR LogonUI VM Preview"

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
    catch {
        return $false
    }
}

Assert-Administrator

if (-not (Test-LooksLikeVirtualMachine) -and -not $AllowNonVm) {
    throw "This experimental Credential Provider is VM-only. No registration was performed."
}

$resolvedDll = (Resolve-Path $DllPath).Path
if ([IO.Path]::GetExtension($resolvedDll) -ne ".dll") {
    throw "DllPath must point to BDFR.LogonUI.CredentialProvider.dll."
}

$credentialProvidersRoot = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers"
$providerKey = Join-Path $credentialProvidersRoot $ProviderClsid
$comKey = "HKLM:\SOFTWARE\Classes\CLSID\$ProviderClsid"
$inprocKey = Join-Path $comKey "InprocServer32"

if (Test-Path $providerKey) {
    throw "BDFR Credential Provider is already registered. Run Unregister-BDFRProvider.ps1 first."
}

$existingProviders = @(Get-ChildItem $credentialProvidersRoot -ErrorAction Stop)
if ($existingProviders.Count -lt 1) {
    throw "No existing Windows credential providers were detected. Registration aborted."
}

$installRoot = Join-Path $env:ProgramFiles "BDFR\LogonUI\CredentialProvider"
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
$installedDll = Join-Path $installRoot "BDFR.LogonUI.CredentialProvider.dll"

$backupRoot = Join-Path $env:ProgramData "BDFR\LogonUI\CredentialProvider"
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFile = Join-Path $backupRoot "credential-providers-$stamp.reg"

$regArgs = @(
    "export",
    "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers",
    $backupFile,
    "/y"
)
& reg.exe @regArgs | Out-Null

Copy-Item $resolvedDll $installedDll -Force

New-Item -Path $providerKey -Force | Out-Null
Set-Item -Path $providerKey -Value $ProviderName

New-Item -Path $comKey -Force | Out-Null
Set-Item -Path $comKey -Value "BDFR LogonUI Credential Provider"

New-Item -Path $inprocKey -Force | Out-Null
Set-Item -Path $inprocKey -Value $installedDll
New-ItemProperty -Path $inprocKey -Name "ThreadingModel" -PropertyType String -Value "Apartment" -Force | Out-Null

Write-Host ""
Write-Host "BDFR Credential Provider VM preview registered." -ForegroundColor Green
Write-Host "CLSID:  $ProviderClsid"
Write-Host "DLL:    $installedDll"
Write-Host "Backup: $backupFile"
Write-Host ""
Write-Host "IMPORTANT:" -ForegroundColor Yellow
Write-Host "- Do not disable any Microsoft credential provider."
Write-Host "- Sign out from the VM console to test the BDFR tile."
Write-Host "- This milestone does not authenticate; use a built-in Microsoft sign-in option."
Write-Host "- Keep the pre-bdfr-cp VM checkpoint until testing is complete."
