# GREE-ALICE-32/33: Yandex Smart Home offline API skeleton

## Purpose

This stage adds an offline-only Yandex Smart Home DTO mapping layer and the first isolated HTTP API skeleton for the future `GreeAliceBridge`.

The API project is separate from production `AssistantEngineer.Api`:

```text
src/Integrations/GreeAliceBridge/AssistantEngineer.GreeAliceBridge.Api
```

## Endpoints

```text
GET  /health
GET  /v1.0/user/devices
POST /v1.0/user/devices/query
POST /v1.0/user/devices/action
POST /v1.0/user/unlink
```

## Offline behavior

```text
GET /health returns healthy/offline-fixture state.
GET /v1.0/user/devices returns dummy-gree-ac-001.
POST /v1.0/user/devices/query returns static offline fixture state.
POST /v1.0/user/devices/action returns dry-run-fail-closed.
POST /v1.0/user/unlink returns an offline unlink result.
```

The `/action` endpoint is request-shape only. It does not send commands outside the offline fixture boundary.

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

## Safety result flags

Action responses must keep:

```text
Status: dry-run-fail-closed
Sent to Gree+ Cloud: false
Sent to MQTT: false
Sent to device: false
Runtime mode: offline-fixture
```

Unlink responses must not clear production AssistantEngineer data.
