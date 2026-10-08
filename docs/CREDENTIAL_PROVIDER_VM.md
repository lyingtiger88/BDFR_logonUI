# Credential Provider V2 — VM Test Plan

## Recommended VM

Primary environment:

- Hyper-V;
- Generation 2 VM;
- Windows 11 x64;
- 4 GB RAM or more;
- 2 virtual CPUs;
- 64 GB virtual disk;
- Secure Boot enabled;
- one local test account with a known password.

Secondary compatibility environment:

- Windows 10 22H2 x64 VM.

VMware Workstation is an acceptable fallback.

## Before registration

1. Create a local test account with a password.
2. Confirm normal Microsoft password sign-in works.
3. Keep Password/PIN/Windows Hello providers enabled.
4. Use the VM console.
5. In Hyper-V, prefer Basic Session for LogonUI tests.
6. Create a checkpoint named `pre-bdfr-cp`.

Do not continue without the checkpoint.

## Build branch

`experimental/credential-provider-v2`

CI builds the x64 DLL but never registers it.

## Register in the VM

Open elevated PowerShell:

    Set-ExecutionPolicy -Scope Process Bypass
    .\Register-BDFRProvider.ps1 -DllPath .\BDFR.LogonUI.CredentialProvider.dll
    .\Test-BDFRProviderRegistration.ps1

The script refuses non-VM systems by default, backs up the Credential Providers
registry branch, registers only the BDFR CLSID, and never disables Microsoft
providers.

## First test

Sign out from inside the VM.

Expected:

- normal Microsoft sign-in options still exist;
- a `BDFR LogonUI — VM Preview` tile appears;
- selecting it displays preview information;
- BDFR cannot authenticate yet;
- switching to the built-in Microsoft provider still signs in normally.

## Remove

After signing in with a Microsoft provider:

    .\Unregister-BDFRProvider.ps1

Sign out again and confirm the BDFR tile is gone.

## Recovery

If LogonUI behaves unexpectedly:

1. do not modify Windows authentication binaries;
2. do not remove Microsoft provider registry keys;
3. power off the VM if necessary;
4. apply the `pre-bdfr-cp` checkpoint.

## Promotion gate

Password serialization is not added until:

- the DLL builds cleanly in CI;
- the tile appears on a Windows 11 VM;
- built-in sign-in remains usable;
- unregister removes the tile;
- reboot with BDFR registered preserves built-in sign-in;
- Windows 10 repeats the same result.
