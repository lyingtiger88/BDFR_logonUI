# Security and Recovery Model

BDFR LogonUI operates near a sensitive Windows boundary. The project therefore treats recoverability as a functional requirement, not an optional hardening task.

## Hard boundaries

The project must not:

- replace or patch `LogonUI.exe`;
- patch `authui.dll`, Winlogon, Windows Hello, LSA, or authentication packages;
- inject DLLs into Windows sign-in processes;
- intercept or log passwords, PINs, biometric material, or credential serialization;
- make successful sign-in depend on the BDFR broker, network connectivity, or a theme/widget provider;
- remove every Microsoft credential provider.

## Authentication ownership

Windows owns authentication. The future Credential Provider V2 component is only an adapter into supported Windows authentication flows.

The widget broker and lock experience are strictly separated from credential handling.

## Recovery requirements before Credential Provider testing

Before registration on any machine:

1. Test installation and removal in a disposable VM.
2. Keep at least one known-good Microsoft sign-in provider available.
3. Document the provider CLSID and exact removal procedure.
4. Provide a recovery script/tool that unregisters only BDFR components.
5. Verify access through Safe Mode / Windows Recovery Environment procedures.
6. Confirm the machine has a known working local or Microsoft account recovery path.
7. Never make the custom provider the only path to unlock during development.

## Broker security

The broker should:

- accept local clients only;
- use Named Pipe ACLs to restrict publishers;
- validate provider identity;
- apply payload size and rate limits;
- reject unknown mandatory schema versions;
- avoid storing sensitive content on disk by default;
- redact notification bodies and secrets from logs;
- expire stale state automatically.

## Lock-screen privacy

Default policy for new providers is conservative:

- Public: visible while locked.
- Private: show only app/title/count unless explicitly allowed.
- Secret: never shown while locked.

Providers cannot override the user's stricter privacy policy.

## Logging

Logs may include:

- provider identifier;
- event category;
- schema version;
- success/failure result;
- timing and bounded diagnostic codes.

Logs must not include:

- passwords or PINs;
- authentication blobs;
- notification message bodies classified Private/Secret;
- tokens, cookies, API keys, or secrets.

## Update safety

Every component close to sign-in must be versioned independently and support rollback. UI/theme updates must never require replacing the Credential Provider binary.

## Threat model priorities

Initial priorities:

1. Prevent lock-screen information disclosure.
2. Prevent untrusted local processes from impersonating trusted providers.
3. Prevent broker failure from affecting sign-in.
4. Prevent malformed provider payloads from crashing the experience.
5. Maintain an independent recovery path if experimental sign-in integration fails.
