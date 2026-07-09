# GREE-ALICE control pilot approval checklist

Approval status: NOT APPROVED

This checklist is a manual gate before any control pilot. The default decision is fail-closed.

## Required checklist

- Repository clean and synced.
- All tests pass.
- Bridge remains isolated.
- Live read-only pilot approved separately.
- Control adapter remains fail-closed until explicit approval.
- MQTT remains blocked.
- No production deployment wiring.
- No production runtime wiring.
- No secrets in repo.
- Credentials stored only outside repo.
- Device/account identifiers masked in evidence.
- Exact single test account approved.
- Exact single test device approved.
- Exact command list approved.
- Temperature limits approved.
- Mode limits approved.
- Fan/swing limits approved.
- Rate limits approved.
- Audit event format approved.
- Kill-switch plan documented.
- Rollback plan documented.
- Manual operator approval recorded.

## Decision

```text
Approval status: NOT APPROVED
Live control: blocked
Control adapter: fail-closed
Single-device pilot: not approved
MQTT: blocked
Production wiring: blocked
```

Do not add credentials, live HTTP control calls, MQTT calls, device control, deployment wiring, or migrations until a later explicit safety approval stage.
