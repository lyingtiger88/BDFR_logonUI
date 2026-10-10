# BDFR LogonUI — True Lock Mode

True Lock Mode connects the BDFR lock experience to the Windows secure sign-in path without patching Windows authentication binaries.

## Flow

1. Windows is configured to skip the stock decorative lock screen.
2. BDFR Credential Provider is registered and assigned as the default credential provider.
3. When LogonUI activates for lock/unlock, the provider launches the installed BDFR experience with `--secure-lock` on the current LogonUI desktop.
4. BDFR runs borderless/full-screen with editing controls hidden.
5. Unlock / Enter / Escape closes the visual BDFR surface and reveals Windows authentication.
6. Password, PIN and Windows Hello remain Windows-owned providers and are not removed.

## Supported Windows customization

The installer scripts use the Windows DefaultCredentialProvider policy. On editions where the optional Client-EmbeddedLogon / Custom Logon feature is available, True Lock additionally suppresses selected stock Welcome UI elements.

## Recovery

Initial physical-machine testing always creates a SYSTEM scheduled auto-rollback. It restores the previous policy values, removes only the BDFR provider registration, and leaves Microsoft credential providers untouched.

## Security boundary

BDFR does not patch LogonUI.exe, AuthUI, Winlogon or LSA and does not collect the user's Windows password or PIN in the secure visual experience.
