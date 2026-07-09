# GREE-ALICE-39: offline end-to-end bridge flow

## Purpose

This stage adds offline end-to-end bridge flow tests for the isolated `GreeAliceBridge`.

The tests cover the safe offline path:

```text
Yandex-style request
        -> isolated bridge API endpoint
        -> safety decision service
        -> offline registry / offline adapters / offline mapper
        -> Yandex-style response
```

## Covered flows

```text
GET  /health
GET  /v1.0/user/devices
POST /v1.0/user/devices/query
POST /v1.0/user/devices/action
POST /v1.0/user/unlink
```

The flow tests verify:

```text
health reports offline-safe state;
device discovery returns dummy-gree-ac-001;
query returns controlled offline state for known and unknown devices;
action remains dry-run fail-closed;
unlink remains offline-only and does not clear production data.
```

## Safety guarantees

The E2E tests also verify that:

```text
No live Gree+ Cloud calls are enabled.
No MQTT CONNECT is enabled.
No MQTT SUBSCRIBE is enabled.
No MQTT PUBLISH is enabled.
No device control is enabled.
No production AssistantEngineer.Api call is part of the bridge flow.
No Telegram runtime call is part of the bridge flow.
No deployment changes are added.
No migrations are added.
```

## Action behavior

Known devices, unknown devices, and unsupported capabilities all return:

```text
Status: dry-run-fail-closed
Sent to Gree+ Cloud: false
Sent to MQTT: false
Sent to device: false
```

## Next stage

The next stage may prepare an isolated staging deploy skeleton, still without production wiring.
