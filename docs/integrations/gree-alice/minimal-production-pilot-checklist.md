# GREE-ALICE minimal production pilot checklist

Approval status: NOT APPROVED

This checklist is a manual gate before any minimal production pilot. The default decision is fail-closed.

## Required checklist

- Repository clean and synced.
- All tests pass.
- Bridge remains isolated.
- Production wiring reviewed and still disabled.
- Live read-only pilot approved separately.
- Control approval remains separate.
- MQTT remains blocked.
- No secrets in repository.
- Credentials stored only outside repository.
- Evidence masks account/device identifiers.
- Exact operator approved.
- Exact account scope approved.
- Exact home scope approved.
- Exact device or child-unit scope approved.
- Read-only-first accepted.
- Audit event format approved.
- Monitoring plan documented.
- Kill-switch plan documented.
- Rollback plan documented.
- Manual approval recorded.

## Decision

```text
Approval status: NOT APPROVED
Minimal production pilot: blocked
Production deployment wiring: disabled
Live read-only pilot: disabled unless separately approved
Live control: disabled
MQTT: blocked
Secrets in repository: forbidden
```

Do not add live Gree+ runtime, live control, MQTT, production wiring, deployment changes, runtime secret config, or migrations until a later explicit safety approval stage.
