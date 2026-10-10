[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProviderClsid = "{55C142FF-F7C7-4A89-B6F1-1D64855CD366}"

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell window."
    }
}

Assert-Administrator

$providerKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\$ProviderClsid"
$comKey = "HKLM:\SOFTWARE\Classes\CLSID\$ProviderClsid"
$installRoot = Join-Path $env:ProgramFiles "BDFR\LogonUI\CredentialProvider"
$installedDll = Join-Path $installRoot "BDFR.LogonUI.CredentialProvider.dll"

if (Test-Path $providerKey) {
    Remove-Item -Path $providerKey -Recurse -Force
}

if (Test-Path $comKey) {
    Remove-Item -Path $comKey -Recurse -Force
}

$removedDll = $false
if (Test-Path $installedDll) {
    try {
        Remove-Item -Path $installedDll -Force
        $removedDll = $true
    }
    catch {
        Write-Warning "Registry rollback succeeded, but the DLL is still loaded. Reboot the VM and delete: $installedDll"
    }
}

if ((Test-Path $providerKey) -or (Test-Path $comKey)) {
    throw "BDFR registry keys could not be fully removed."
}

Write-Host ""
Write-Host "BDFR Credential Provider registration removed." -ForegroundColor Green
Write-Host "Microsoft credential providers were not modified."
if ($removedDll) { Write-Host "Experimental DLL removed." }
else { Write-Host "If the DLL is still present, reboot the VM before deleting it." }
