[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$ProviderClsid = "{55C142FF-F7C7-4A89-B6F1-1D64855CD366}"
$providerKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\$ProviderClsid"
$comKey = "HKLM:\SOFTWARE\Classes\CLSID\$ProviderClsid"
$installRoot = Join-Path $env:ProgramFiles "BDFR\LogonUI\CredentialProvider"
$installedDll = Join-Path $installRoot "BDFR.LogonUI.CredentialProvider.dll"
$stateRoot = Join-Path $env:ProgramData "BDFR\LogonUI\CredentialProvider"
$logPath = Join-Path $stateRoot "physical-lab-recovery.log"

New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null

function Write-RecoveryLog([string]$Message) {
    $line = "$(Get-Date -Format o)  $Message"
    Add-Content -Path $logPath -Value $line -Encoding UTF8
}

Write-RecoveryLog "Starting BDFR physical-lab auto rollback."

try {
    if (Test-Path $providerKey) {
        Remove-Item -Path $providerKey -Recurse -Force
        Write-RecoveryLog "Credential Provider registration key removed."
    }
}
catch { Write-RecoveryLog "Provider-key removal failed: $($_.Exception.Message)" }

try {
    if (Test-Path $comKey) {
        Remove-Item -Path $comKey -Recurse -Force
        Write-RecoveryLog "COM registration key removed."
    }
}
catch { Write-RecoveryLog "COM-key removal failed: $($_.Exception.Message)" }

try {
    if (Test-Path $installedDll) {
        Remove-Item -Path $installedDll -Force
        Write-RecoveryLog "Credential Provider DLL removed."
    }
}
catch {
    Write-RecoveryLog "DLL is probably still loaded; registry rollback already prevents future loads. $($_.Exception.Message)"
}

Write-RecoveryLog "BDFR physical-lab rollback finished. Microsoft providers were not modified."
