# GREE-ALICE live read-only pilot approval checklist

Approval status: NOT APPROVED

This checklist is a manual gate before any live read-only Gree+ adapter code or pilot run.

The default decision is fail-closed: no live read-only pilot is approved until a later explicit safety stage changes this status.

## Required checklist

- Repository clean and synced.
- All tests pass.
- Bridge remains isolated.
- Control adapter remains blocked.
- MQTT remains blocked.
- No production deployment wiring.
- No production runtime wiring.
- No secrets in repo.
- Credentials stored only outside repo.
- Device/account identifiers masked in evidence.
- Operator approves exact test account/device.
- Kill-switch plan documented.
- Rollback plan documented.
- Live pilot limited to read-only.
- Device control remains blocked.
- Gree+ runtime control remains blocked.
- MQTT CONNECT remains blocked.
- MQTT SUBSCRIBE remains blocked.
- MQTT PUBLISH remains blocked.

## Decision

```text
Approval status: NOT APPROVED
Live read-only pilot: blocked
Live control: blocked
MQTT: blocked
Production wiring: blocked
```

Do not add credentials, live HTTP calls, MQTT calls, device control, deployment wiring, or migrations until a later explicit safety approval stage.
