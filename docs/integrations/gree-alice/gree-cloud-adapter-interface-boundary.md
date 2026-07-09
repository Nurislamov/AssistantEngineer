# GREE-ALICE-36: Gree Cloud adapter interface boundary

## Purpose

This stage adds a safe adapter interface boundary for future Gree+ Cloud integration work.

The current implementation is offline-fake only. It prepares the application layer for future read-only and control adapters without adding any live calls, network access, MQTT, device control, deployment, or production runtime wiring.

## Adapter layers

```text
Yandex API / Bridge Application
        ↓
Gree Cloud adapter interfaces
        ↓
offline fake adapter
        ↓
future real Gree+ Cloud adapter in a separate safety stage
```

## Current adapter mode

```text
Adapter mode: offline-fake
Uses live Gree+ Cloud: false
Uses HTTP network: false
Uses MQTT network: false
Allows MQTT CONNECT: false
Allows MQTT SUBSCRIBE: false
Allows MQTT PUBLISH: false
Allows device control: false
Allows runtime control: false
```

## Read adapter behavior

The read adapter reads only the offline fixture registry from `GREE-ALICE-35`.

It can return offline descriptors and fixture state for:

```text
dummy-gree-ac-001
dummy-vrf-gateway-001
dummy-vrf-child-001
```

Unknown devices return a controlled `offline-unknown` state.

## Control adapter behavior

The control adapter is a boundary only and always fails closed.

```text
Status: dry-run-fail-closed
Sent to Gree+ Cloud: false
Sent to MQTT: false
Sent to device: false
Adapter mode: offline-fake
```

Known devices, unknown devices, and unknown capabilities all keep the same fail-closed behavior.

## Explicitly not implemented

```text
No live Gree+ Cloud calls.
No HttpClient live calls.
No MQTT CONNECT.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
No production runtime wiring.
No deployment changes.
No migrations.
```

A real adapter implementation requires a separate future safety stage.
