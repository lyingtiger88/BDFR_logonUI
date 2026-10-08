[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$system = Get-CimInstance Win32_ComputerSystem
$os = Get-CimInstance Win32_OperatingSystem
$credentialProvidersRoot = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers"
$providers = @(Get-ChildItem $credentialProvidersRoot -ErrorAction Stop)

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
$looksVirtual = ("$($system.Manufacturer) $($system.Model)" -match "Virtual|VMware|VirtualBox|KVM|QEMU|Hyper-V")

[pscustomobject]@{
    ComputerName = $env:COMPUTERNAME
    Manufacturer = $system.Manufacturer
    Model = $system.Model
    Windows = $os.Caption
    Version = $os.Version
    Build = $os.BuildNumber
    Is64BitOS = [Environment]::Is64BitOperatingSystem
    RunningElevated = $isAdmin
    LooksVirtual = $looksVirtual
    ExistingCredentialProviderKeys = $providers.Count
    RecommendedForPhysicalLab = ((-not $looksVirtual) -and $isAdmin -and [Environment]::Is64BitOperatingSystem -and $providers.Count -ge 2)
}
