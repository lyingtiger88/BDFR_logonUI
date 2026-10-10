[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProviderClsid = "{55C142FF-F7C7-4A89-B6F1-1D64855CD366}"
$credentialProvidersRoot = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers"
$providerKey = Join-Path $credentialProvidersRoot $ProviderClsid
$comKey = "HKLM:\SOFTWARE\Classes\CLSID\$ProviderClsid\InprocServer32"

$system = Get-CimInstance Win32_ComputerSystem
$existingProviders = @(Get-ChildItem $credentialProvidersRoot -ErrorAction Stop)

$dllPath = $null
if (Test-Path $comKey) { $dllPath = (Get-Item $comKey).GetValue("") }

[pscustomobject]@{
    Manufacturer = $system.Manufacturer
    Model = $system.Model
    LooksVirtual = ("$($system.Manufacturer) $($system.Model)" -match "Virtual|VMware|VirtualBox|KVM|QEMU|Hyper-V")
    BDFRProviderRegistered = (Test-Path $providerKey)
    BDFRComRegistered = (Test-Path $comKey)
    BDFRDllPath = $dllPath
    BDFRDllExists = ($dllPath -and (Test-Path $dllPath))
    CredentialProviderKeyCount = $existingProviders.Count
}
