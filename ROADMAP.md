# BDFR LogonUI Roadmap

This roadmap is intentionally recovery-first. The project will build the lock dashboard and integration layer before touching Windows sign-in integration.

## Milestone 0 — Repository foundation
**Status: in progress**

- [x] Bootstrap repository and define project goals.
- [x] Establish no-patching / no-injection security boundary.
- [ ] Add architecture and security documentation.
- [ ] Add shared contracts project.
- [ ] Add broker prototype.
- [ ] Add CI build workflow after the first buildable solution exists.

**Exit criteria:** repository structure is stable enough for incremental development.

## Milestone 1 — Contracts + local broker
**Target: first executable prototype**

- Define versioned widget manifest.
- Define notification model and privacy levels.
- Define provider registration and heartbeat model.
- Build broker as a local Windows service/host.
- Use local authenticated IPC; Named Pipes are the preferred transport.
- Reject unauthenticated or malformed provider payloads.
- Add a read-only mock provider for development.

**Exit criteria:** a sample provider can publish status to the broker and a test client can read it.

## Milestone 2 — Lock Experience shell
**Target: interactive desktop prototype**

- WinUI 3/.NET desktop shell for rapid UI iteration.
- Clock and dual Gregorian/Persian date.
- Full-screen background engine.
- Glass/Acrylic cards with accessibility fallbacks.
- Multi-monitor and DPI-aware layout.
- Widget host with fixed safe zones.
- Power/network/system-status widgets.
- Offline-first startup.

**Exit criteria:** a stable dashboard can render broker data without requiring sign-in integration.

## Milestone 3 — Persian Calendar integration
**Target: integration with the existing calendar application**

- Extract calendar/event provider contract.
- Import Persian date/event data from the existing calendar app.
- Seasonal background provider.
- Support the existing seasonal background naming convention, e.g. `Spring_16x9`.
- Elena Mode integration: automatic seasonal background selection.
- Accent-color selection independent from seasonal mode.
- Reminder/event visibility policy while locked.
- Local event cache for offline display.

**Exit criteria:** calendar, events, seasonal themes, and reminders render from the shared provider model.

## Milestone 4 — Notifications + privacy
**Target: useful lock-screen dashboard**

- Public / private / secret notification levels.
- App-level lock-screen visibility controls.
- Counts/badges without exposing message bodies.
- Notification expiry and deduplication.
- Do-not-disturb rules.
- Per-provider rate limiting.
- User-visible audit page showing which apps may publish to the lock screen.

**Exit criteria:** notifications remain useful without leaking sensitive content before authentication.

## Milestone 5 — System providers
**Target: integrated status center**

- CPU, RAM and storage summaries.
- Network state.
- Battery/power state.
- BDFR Sentinel status adapter.
- Optional adapters for other BDFR applications.
- Provider SDK documentation and sample integration.

**Exit criteria:** third-party/BDFR apps can publish bounded, privacy-aware widgets through a stable SDK.

## Milestone 6 — Windows sign-in integration
**Target: guarded experimental branch**

- Native C++ Credential Provider V2 proof of concept.
- Keep Microsoft password/PIN/Windows Hello providers available.
- Do not store or transmit credentials through the broker.
- Separate authentication code from widget/UI code.
- Recovery instructions prepared before registration.
- Test in a VM before any physical-machine test.
- Support logon/unlock scenarios only after recovery testing passes.

**Exit criteria:** experimental provider works in a VM and can be removed/recovered without losing access.

## Milestone 7 — Enterprise Custom Logon path
**Target: optional Enterprise/Education configuration**

- Detect edition and supported Custom Logon capabilities.
- Optional suppression of selected stock logon UI elements where officially supported.
- Never assume Enterprise-only features on Home/Pro.
- Provide reversible configuration tooling.

**Exit criteria:** supported editions can opt into a cleaner OEM-style experience with one-click rollback.

## Milestone 8 — Installer, recovery and release engineering
**Target: alpha release**

- Signed installer/package plan.
- Install/uninstall/repair flows.
- Safe-mode/recovery documentation.
- Configuration backup and restore.
- Crash-safe broker startup.
- Automatic fallback if custom experience components fail.
- CI build, static analysis, unit tests and packaging.

**Exit criteria:** alpha can be installed and removed reproducibly without risking account access.

---

## Architecture rules

1. Windows remains the authentication authority.
2. BDFR LogonUI never receives plaintext passwords/PINs through the widget broker.
3. Built-in Windows sign-in providers remain available as recovery paths.
4. No patching of `LogonUI.exe`, `authui.dll`, Winlogon, Windows Hello, or LSA.
5. Any component running close to the sign-in boundary must have minimal dependencies.
6. Network content is optional; lock experience must render safely offline.
7. Sensitive notification content defaults to hidden while locked.

## Immediate next commits

1. Architecture/security documents.
2. Shared contracts project.
3. Broker prototype with in-memory registry.
4. Mock calendar/system provider.
5. Experience prototype consuming broker data.
