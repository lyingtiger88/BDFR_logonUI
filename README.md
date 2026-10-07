# BDFR LogonUI

BDFR LogonUI is a Windows lock/logon experience project designed to integrate a custom lock dashboard with BDFR applications while keeping Windows authentication boundaries intact.

## Project goals

- Custom lock experience with clock, Persian/Gregorian calendar, events and reminders.
- Pluggable widgets for system status and BDFR applications.
- Privacy-aware notifications while the machine is locked.
- A local broker service for communication between trusted applications and the lock experience.
- A Windows Credential Provider V2 integration path for supported sign-in scenarios.
- Recovery-first design: built-in Windows sign-in providers remain available as a fallback.

## Non-goals

The project will not patch or replace `LogonUI.exe`, hook Winlogon, inject code into LogonUI, or replace Windows authentication libraries. Authentication remains owned by Windows/LSA.

## Planned components

- `BDFR.LogonUI.Contracts` — shared widget/notification contracts.
- `BDFR.LogonUI.Broker` — Windows service / local broker.
- `BDFR.LogonUI.Experience` — lock dashboard UI.
- `BDFR.LogonUI.Settings` — configuration and privacy controls.
- `BDFR.LogonUI.CredentialProvider` — native Credential Provider V2 adapter.
- `providers/` — calendar, system status, notifications, and BDFR app providers.

See [ROADMAP.md](ROADMAP.md) and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) as the project evolves.

## Status

Early architecture and scaffolding.
