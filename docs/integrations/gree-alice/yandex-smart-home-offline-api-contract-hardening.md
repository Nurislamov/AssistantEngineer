# GREE-ALICE-34: Yandex Smart Home offline API contract hardening

## Purpose

This stage hardens the offline-only Yandex Smart Home API skeleton before any account, registry, or live control work.

The bridge remains isolated under:

```text
src/Integrations/GreeAliceBridge
```

## Hardened response contract

Offline responses now preserve stable concepts for future integration work:

```text
request_id
status
error_code
message
device_id
capability
runtime_mode
sent_to_gree_cloud
sent_to_mqtt
sent_to_device
```

Existing DTO names remain stable. The contract is extended with offline metadata and controlled error fields.

## Defined offline behavior

```text
/devices always returns dummy-gree-ac-001.
/query for dummy-gree-ac-001 returns offline fixture state.
/query for an unknown device returns offline unknown state.
/query with an empty request returns a controlled offline-empty-query response.
/action for known, unknown, or unsupported capabilities returns dry-run-fail-closed.
/action with an empty request returns a controlled offline-empty-action response.
/unlink returns an offline unlink result and does not clear production AssistantEngineer data.
```

## Fail-closed action guarantee

Every action response remains:

```text
Status: dry-run-fail-closed
Sent to Gree+ Cloud: false
Sent to MQTT: false
Sent to device: false
Runtime mode: offline-fixture
```

## Explicitly not implemented

```text
No live Gree+ Cloud calls.
No MQTT CONNECT.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
No production runtime wiring.
No deployment changes.
No migrations.
```
