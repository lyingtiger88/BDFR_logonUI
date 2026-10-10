[CmdletBinding()]
param(
    [string]$ExperiencePath = "$env:ProgramFiles\BDFR\LogonUI\BDFR.LogonUI.Demo.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$os = Get-CimInstance Win32_OperatingSystem
$providers = @(
    Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers"
)

$customLogon = $false
$customLogonState = "Unavailable"
try {
    $feature = Get-WindowsOptionalFeature -Online -FeatureName Client-EmbeddedLogon -ErrorAction Stop
    $customLogon = $true
    $customLogonState = [string]$feature.State
}
catch {}

[pscustomobject]@{
    ComputerName = $env:COMPUTERNAME
    WindowsCaption = $os.Caption
    WindowsVersion = $os.Version
    BuildNumber = $os.BuildNumber
    X64 = [Environment]::Is64BitOperatingSystem
    CredentialProviderCount = $providers.Count
    ExperienceFound = Test-Path $ExperiencePath
    ExperiencePath = $ExperiencePath
    CustomLogonSupported = $customLogon
    CustomLogonState = $customLogonState
    RecommendedForTrueLockTest = (
        [Environment]::Is64BitOperatingSystem -and
        [Version]$os.Version -ge [Version]"10.0.17763" -and
        $providers.Count -ge 2 -and
        (Test-Path $ExperiencePath)
    )
} | Format-List
