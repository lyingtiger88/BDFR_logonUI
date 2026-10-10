# BDFR LogonUI Credential Provider V2

This directory contains the guarded native Credential Provider experiment.

## Current milestone: VM preview tile

The current build is intentionally **non-authenticating**.

It does:

- implement `ICredentialProvider`;
- implement `ICredentialProviderSetUserArray`;
- expose an `ICredentialProviderCredential2` tile;
- support `CPUS_LOGON` and `CPUS_UNLOCK_WORKSTATION`;
- enumerate the first Windows user supplied by LogonUI;
- return that user's SID;
- display a BDFR LogonUI preview tile.

It does **not**:

- collect a password or PIN;
- serialize credentials;
- replace Windows Hello;
- hide or disable Microsoft's providers;
- use the BDFR broker for credentials;
- register itself automatically.

`GetSerialization` deliberately returns `CPGSR_NO_CREDENTIAL_NOT_FINISHED`.
Authentication must still be performed with a built-in Microsoft sign-in option.

This first milestone validates native DLL loading, COM registration, user-tile
rendering, Logon/Unlock scenarios, safe removal, and VM recovery.

## Toolchain

- Visual Studio 2022 Build Tools / MSVC v143
- Windows SDK
- x64
- C++23

## Provider CLSID

`{55C142FF-F7C7-4A89-B6F1-1D64855CD366}`

Do not change the CLSID after test machines have registered the provider
without first unregistering the old CLSID.

## Safety

Do not register this DLL on a physical machine during the experimental phase.
Use only the scripts under `scripts/credential-provider-vm/`.

The implementation follows the Windows Credential Provider V2 interface
pattern demonstrated by Microsoft's official Windows classic Credential
Provider sample, while keeping BDFR's first milestone non-authenticating.
