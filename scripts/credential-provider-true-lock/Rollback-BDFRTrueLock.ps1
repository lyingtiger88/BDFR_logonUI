[CmdletBinding()]
param(
    [string]$StatePath = "$env:ProgramData\BDFR\LogonUI\CredentialProvider\true-lock-state.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$ProviderClsid = "{55C142FF-F7C7-4A89-B6F1-1D64855CD366}"
$TaskName = "BDFR LogonUI True Lock Auto Rollback"
$providerKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\$ProviderClsid"
$comKey = "HKLM:\SOFTWARE\Classes\CLSID\$ProviderClsid"

function Restore-RegistryValue {
    param($Snapshot)
    if ($null -eq $Snapshot) { return }

    $path = [string]$Snapshot.Path
    $name = [string]$Snapshot.Name

    if ([bool]$Snapshot.Exists) {
        New-Item -Path $path -Force | Out-Null
        $kind = if ($Snapshot.Kind) { [string]$Snapshot.Kind } else { "String" }
        New-ItemProperty -Path $path -Name $name -Value $Snapshot.Value -PropertyType $kind -Force | Out-Null
    }
    else {
        Remove-ItemProperty -Path $path -Name $name -ErrorAction SilentlyContinue
    }
}

try {
    if (Test-Path $StatePath) {
        $state = Get-Content -Raw -Path $StatePath | ConvertFrom-Json
        foreach ($snapshot in @($state.RegistryValues)) {
            Restore-RegistryValue $snapshot
        }

        if ($state.CustomLogonFeatureInitiallyEnabled -eq $false -and
            $state.CustomLogonFeatureWasEnabledByInstaller -eq $true) {
            try {
                Disable-WindowsOptionalFeature -Online -FeatureName Client-EmbeddedLogon -NoRestart -ErrorAction Stop | Out-Null
            }
            catch {
                Write-Warning "Could not disable Client-EmbeddedLogon during rollback: $($_.Exception.Message)"
            }
        }
    }
}
catch {
    Write-Warning "State restore encountered an error: $($_.Exception.Message)"
}

Remove-Item -Path $providerKey -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path $comKey -Recurse -Force -ErrorAction SilentlyContinue

$installedDll = "$env:ProgramFiles\BDFR\LogonUI\CredentialProvider\BDFR.LogonUI.CredentialProvider.dll"
Remove-Item -Path $installedDll -Force -ErrorAction SilentlyContinue

Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue

$logRoot = "$env:ProgramData\BDFR\LogonUI\CredentialProvider"
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
"$(Get-Date -Format o) BDFR True Lock rollback completed." | Add-Content -Path (Join-Path $logRoot "true-lock-recovery.log")

Write-Host "BDFR True Lock settings were rolled back. Windows built-in sign-in providers were not removed." -ForegroundColor Green
