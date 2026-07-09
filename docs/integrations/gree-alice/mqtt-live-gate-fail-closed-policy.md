# GREE-ALICE-26: live-gate fail-closed policy

## Purpose

This policy defines the default safety behavior for any future GREE-ALICE live MQTT or control work.

## Default state

```text
Default live CONNECT permission: false
Default SUBSCRIBE permission: false
Default PUBLISH permission: false
Default device control permission: false
Default runtime integration permission: false
```

## Fail-closed rules

```text
If required evidence is missing, block.
If approval is missing, block.
If operator sign-off is incomplete, block.
If a raw secret is detected in an output path, block.
If target device identity is ambiguous, block.
If command payload semantics are uncertain, block.
If state feedback cannot be verified, block.
If rate-limit or retry behavior is unknown, block.
```

## Kill-switch requirements for future runtime

Any later runtime bridge must have these controls before production:

```text
Global bridge disabled by default.
Per-account control disabled by default.
Per-device control disabled by default.
Per-capability control disabled by default.
Emergency disable path documented.
No secret values in logs.
No raw payload values in logs.
No control retry storm.
```

## Forbidden in this stage

```text
No MQTT CONNECT implementation.
No MQTT SUBSCRIBE implementation.
No MQTT PUBLISH implementation.
No device control implementation.
No AssistantEngineer.Api integration.
No Telegram integration.
No runtime configuration.
No deployment change.
No migration.
```

## Future release gate

A production release must remain blocked until the bridge has explicit account-level and device-level enablement, masked logs, health checks, rollback instructions, and a tested disable procedure.
