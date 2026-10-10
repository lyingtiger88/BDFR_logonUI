# BDFR LogonUI Windows Installer

The official preview installer is built as `BDFR_LogonUI_Setup.exe`.

## Install

Run the setup executable as Administrator. The default target is:

`C:\Program Files\BDFR\LogonUI`

The installer deploys:

- `BDFR.LogonUI.Demo.exe`
- `BDFR.LogonUI.Broker.exe`
- default sidecar resources

It creates standard Windows uninstall metadata and can optionally create a desktop shortcut.

## Upgrade / executable replacement

Run a newer `BDFR_LogonUI_Setup.exe` over an existing installation.

The installer uses the same application ID and previous install directory, so it upgrades in place. It asks Windows Restart Manager to close running BDFR Demo/Broker processes and replaces the BDFR executables automatically.

If a BDFR executable is still locked, the installer can schedule replacement at restart.

Before replacement, the currently installed BDFR executables are copied to:

`%ProgramData%\BDFR\LogonUI\InstallerBackup`

The organizational message file uses `onlyifdoesntexist`, so an existing customized message is not overwritten by a normal upgrade.

## Silent upgrade

For managed testing:

`BDFR_LogonUI_Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART`

## Security boundary

This installer replaces only BDFR-owned files. It does not replace or patch Windows `LogonUI.exe`, Winlogon, LSA, AuthUI, Password/PIN/Windows Hello providers, or other Windows authentication binaries.
