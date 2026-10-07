# Architecture

## Overview

BDFR LogonUI is split into a presentation layer, a local broker, provider adapters, and a narrowly scoped Windows sign-in adapter.

```text
BDFR apps / providers
        |
        | authenticated local IPC
        v
+-------------------------+
| BDFR.LogonUI.Broker     |
| - provider registry     |
| - privacy filtering     |
| - state cache           |
| - rate limiting         |
+-------------------------+
        |
        | read-only snapshots
        v
+-------------------------+
| Lock Experience         |
| - clock/date            |
| - Persian calendar      |
| - reminders             |
| - notifications         |
| - system status         |
| - themes/backgrounds    |
+-------------------------+

Windows sign-in path (separate trust boundary)

+-------------------------+
| Credential Provider V2  |
+-------------------------+
        |
        v
Windows LogonUI / LSA
```

The Credential Provider does not consume application notifications and the Broker never receives user credentials.

## Components

### BDFR.LogonUI.Contracts

Pure data contracts shared by the broker and provider SDK.

Initial contracts:

- `WidgetManifest`
- `WidgetSnapshot`
- `LockNotification`
- `PrivacyLevel`
- `ProviderRegistration`
- `ProviderHeartbeat`

Contracts are versioned. Unknown optional fields must be ignored by readers to preserve forward compatibility.

### BDFR.LogonUI.Broker

A local service responsible for:

- validating provider identity and input;
- maintaining bounded in-memory state;
- applying privacy policy;
- exposing sanitized read-only snapshots to the lock experience;
- expiring stale providers/widgets;
- recording minimal diagnostics without sensitive payloads.

The preferred IPC transport is Windows Named Pipes with explicit ACLs. The first prototype may use an in-process transport interface so unit tests do not require service installation.

### BDFR.LogonUI.Experience

The visual lock dashboard.

Responsibilities:

- responsive full-screen layout;
- DPI and multi-monitor awareness;
- clock and Persian/Gregorian calendar presentation;
- seasonal backgrounds and theme selection;
- safe rendering of sanitized widget data;
- graceful degradation when broker/providers are unavailable.

The experience must never be a credential collection surface.

### BDFR.LogonUI.Settings

User-facing configuration:

- widget enable/disable;
- lock-screen privacy controls;
- theme/background settings;
- Elena Mode and seasonal background behavior;
- provider permissions;
- diagnostics and recovery status.

### Providers

Providers publish bounded data to the broker.

Planned first-party providers:

- Persian Calendar
- System Status
- Notifications
- BDFR Sentinel

Provider code runs outside the authentication path.

### BDFR.LogonUI.CredentialProvider

Native C++ Credential Provider V2 adapter.

Rules:

- keep dependencies minimal;
- use Windows-supported interfaces only;
- never patch or inject into LogonUI/Winlogon;
- never disable every built-in credential provider;
- no network calls;
- no dependency on the Broker for successful sign-in;
- registration and removal must be reversible.

## Data flow

### Widget publication

1. Provider establishes authenticated local IPC.
2. Broker validates provider registration.
3. Provider publishes a `WidgetSnapshot`.
4. Broker validates size, schema version, TTL, and privacy classification.
5. Broker stores a bounded snapshot.
6. Experience requests a sanitized read-only view.
7. Broker applies lock-state privacy policy before returning data.

### Notification publication

Notifications carry a privacy level:

- `Public` — title/body may be shown while locked.
- `Private` — app/title or count may be shown; content is hidden.
- `Secret` — hidden entirely until unlocked.

The default for new providers is `Private`.

## Calendar integration

The existing Persian Calendar application is treated as a provider, not embedded directly into the broker.

The provider exposes:

- Persian date;
- Gregorian date;
- events and holidays;
- reminders;
- seasonal/theme state;
- optional background asset selection.

Seasonal background discovery follows the existing convention where possible:

```text
theme/
  season backgrounds/
    Spring_16x9.jpg
    Summer_16x9.jpg
    Autumn_16x9.jpg
    Winter_16x9.jpg
```

Elena Mode chooses the seasonal background automatically. Accent selection remains independently configurable.

## Failure model

- Broker unavailable -> experience displays clock/date and cached safe data.
- Provider unavailable -> its widget expires after TTL.
- Corrupt provider payload -> reject only that payload/provider.
- Credential Provider failure -> built-in Windows providers remain available.
- Experience crash -> must not block Windows authentication.
- Network unavailable -> no impact on sign-in.

## Technology direction

- .NET 10 for broker/contracts/settings and early UI prototypes.
- WinUI 3 for the desktop experience/settings surface.
- C++23/Win32/COM for Credential Provider V2.
- Named Pipes for local IPC.
- JSON only at non-secret provider/broker boundaries where appropriate; credential serialization follows Windows APIs and remains isolated.

## Repository layout

```text
BDFR_logonUI/
  docs/
  src/
    BDFR.LogonUI.Contracts/
    BDFR.LogonUI.Broker/
    BDFR.LogonUI.Experience/
    BDFR.LogonUI.Settings/
    BDFR.LogonUI.CredentialProvider/
  providers/
    PersianCalendar/
    SystemStatus/
    Notifications/
  tests/
  installer/
```
