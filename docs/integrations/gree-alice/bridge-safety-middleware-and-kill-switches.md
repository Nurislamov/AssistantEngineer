# GREE-ALICE-38: bridge safety middleware and kill switches

## Purpose

This stage adds centralized safety contracts, kill-switch defaults, and an offline-only safety decision service for the isolated `GreeAliceBridge`.

The safety layer is registered only in the isolated bridge API and does not touch production `AssistantEngineer.Api`, Telegram, deployment, runtime configuration, or migrations.

## Default policy

```text
Runtime mode: offline-fixture
Read-only fixture queries: allowed
Offline device discovery: allowed
Offline unlink: allowed
Action dry-run: allowed
Live Gree+ Cloud: blocked
Live HTTP network: blocked
MQTT CONNECT: blocked
MQTT SUBSCRIBE: blocked
MQTT PUBLISH: blocked
Device control: blocked
Runtime control: blocked
Production runtime wiring: blocked
```

## Kill switches

Default static kill-switch state:

```text
Global bridge disabled: false
Discovery disabled: false
Query disabled: false
Action disabled: false
Unlink disabled: false
Read adapter disabled: false
Control adapter disabled: true
```

`/action` can still accept offline request shapes, but it remains dry-run/fail-closed and never sends commands.

## Safety decisions

```text
Health: allowed
Discover devices: allowed offline only
Query devices: allowed offline only
Unlink: allowed offline only
Execute action: dry-run fail-closed only
Read adapter: allowed offline only
Control adapter: blocked fail-closed
Live HTTP network: blocked
MQTT CONNECT: blocked
MQTT SUBSCRIBE: blocked
MQTT PUBLISH: blocked
Device control: blocked
Production runtime wiring: blocked
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
No bridge runtime environment configuration.
```
