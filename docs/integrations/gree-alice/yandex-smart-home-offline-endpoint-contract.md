# GREE-ALICE-30: Yandex Smart Home offline endpoint contract skeleton

## Purpose

This document defines the future Yandex Smart Home endpoint contract for the isolated GreeAliceBridge service.

It is contract-only. It does not implement HTTP endpoints and it does not send any request to Gree+ Cloud.

## Future endpoints

```text
GET  /v1.0/user/devices
POST /v1.0/user/devices/query
POST /v1.0/user/devices/action
POST /v1.0/user/unlink
```

## Endpoint behavior for offline skeleton

```text
/v1.0/user/devices returns static fixture devices only.
/v1.0/user/devices/query returns static fixture state only.
/v1.0/user/devices/action returns fail-closed dry-run response only.
/v1.0/user/unlink clears only future bridge session state, not production AssistantEngineer data.
```

## Device capabilities planned for MVP

```text
on/off
mode
temperature setpoint
fan speed
online/offline state
```

## Fail-closed action rule

The offline skeleton must treat every incoming action as a dry-run unless a later explicit control stage enables one account, one test device, and one allowed capability.

```text
Action accepted by skeleton: yes
Action sent to Gree+ Cloud: no
Action sent to MQTT: no
Action sent to device: no
Response mode: dry-run fail-closed
```

## Isolation requirements

```text
No dependency on AssistantEngineer.Api.
No dependency on Telegram bot runtime.
No production database dependency.
No production migrations.
No deployment wiring.
No real credentials in fixtures.
No raw cloud identifiers in fixtures.
```
