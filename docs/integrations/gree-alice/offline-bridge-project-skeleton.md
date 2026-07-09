# GREE-ALICE-31: offline bridge project skeleton

## Purpose

This stage creates the first isolated offline-only bounded context for the future `GreeAliceBridge`.

The skeleton lives under:

```text
src/Integrations/GreeAliceBridge
```

## Projects

```text
AssistantEngineer.GreeAliceBridge.Contracts
AssistantEngineer.GreeAliceBridge.Application
```

Both projects target `net10.0`, enable nullable reference types, and do not add external package dependencies.

## Offline-only boundary

The current runtime mode is:

```text
offline-fixture
```

The application service returns only one dummy device:

```text
dummy-gree-ac-001
```

Unknown device IDs return an offline unknown state. Actions return `dry-run-fail-closed` and do not leave the offline skeleton boundary.

## Explicitly not implemented

```text
No HTTP endpoint implementation.
No live Gree+ Cloud calls.
No MQTT CONNECT.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
No production runtime wiring.
No deployment changes.
No migrations.
```

## Safety gates

```text
Live MQTT CONNECT: false
MQTT SUBSCRIBE: false
MQTT PUBLISH: false
Device control: false
Gree+ runtime control: false
```

## Production data boundary

The offline unlink result does not clear or mutate production AssistantEngineer data.
