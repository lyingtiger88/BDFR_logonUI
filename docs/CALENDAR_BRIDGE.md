# Anahita -> BDFR LogonUI Calendar Bridge

The calendar integration uses a local broker boundary instead of allowing the lock experience to open Anahita's SQLite database directly.

## Data path

```text
Anahita
  |
  | current-user Named Pipe
  | publish-calendar
  v
BDFR.LogonUI.Broker
  |
  | privacy filtering + bounded validation
  | read-calendar
  v
BDFR.LogonUI.Demo / future lock experience
```

Pipe name:

`BDFR.LogonUI.Broker.v1`

Protocol version: **1**

## Published calendar data

Anahita publishes a bounded snapshot containing:

- Persian date label;
- Gregorian date label;
- Hijri date label;
- today's official occasions;
- today's user events;
- today's incomplete tasks;
- pending reminders within the next 24 hours.

The bridge intentionally excludes:

- notes;
- credentials;
- notification tokens;
- arbitrary database rows;
- settings unrelated to the lock experience.

## Privacy defaults

Until Anahita exposes per-item lock-screen privacy in its own data model:

- official occasions -> **Public**;
- user events -> **Private**;
- user tasks -> **Private**;
- reminders -> **Private**.

When the broker is queried for a locked surface:

- Public titles are preserved;
- Private event/task titles become `رویداد خصوصی`;
- Private reminder titles become `یادآوری خصوصی`;
- Secret items are omitted entirely.

The standalone demo always requests the locked view.

## IPC protections

The current prototype applies:

- Windows current-user-only Named Pipe access;
- protocol-version validation;
- provider allow-list validation (`bdfr.anahita`);
- request/provider identity matching;
- 64 KiB request bound before JSON deserialization completes;
- bounded agenda/reminder counts;
- bounded text lengths;
- minimum publish interval;
- broker failure isolation.

This is the initial local authorization boundary. A future Windows-service build may additionally validate publisher executable identity/signature.

## Offline-safe cache

The broker keeps the live snapshot in memory.

For offline continuity it writes a separate cache under the current user's LocalAppData:

`%LocalAppData%\BDFR\LogonUI\broker.calendar.safe.json`

Only the already privacy-filtered lock-safe form is written to disk.

Therefore:

- private titles are never stored in this broker cache;
- secret items are never stored in this broker cache;
- the cache expires automatically;
- cache I/O failure never blocks live broker operation.

## Refresh behavior

Anahita attempts a local publish approximately every 30 seconds.

The standalone LogonUI demo reads the broker approximately every 12 seconds.

If the broker is not running, the demo attempts to launch the packaged
`BDFR.LogonUI.Broker.exe` silently.

If Anahita is not available, the lock experience can use the unexpired safe cache.

## Test coverage

`tools/BDFR.LogonUI.Smoke` starts the real Named Pipe server and verifies:

- provider -> broker -> reader flow;
- Public title visibility;
- Private title masking;
- Secret item removal;
- unlocked reads preserve original data.

GitHub release packaging is gated on this smoke test.
