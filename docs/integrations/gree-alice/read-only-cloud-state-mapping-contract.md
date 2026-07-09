# GREE-ALICE-37: read-only cloud state mapping contract

## Purpose

This stage adds a read-only state mapping contract for future Gree+ Cloud state normalization.

The current input is offline masked fixture data only. No live Gree+ Cloud, HTTP, MQTT, device control, production runtime wiring, deployment, or migration work is added.

## Mapping path

```text
masked Gree+ raw state snapshot
        -> read-only state mapper
        -> normalized bridge state
        -> future Yandex Smart Home query response mapping
```

## Safety boundary

```text
Mapping mode: offline-masked-fixture
Uses masked input only: true
Uses live Gree+ Cloud: false
Uses HTTP network: false
Uses MQTT network: false
Allows MQTT CONNECT: false
Allows MQTT SUBSCRIBE: false
Allows MQTT PUBLISH: false
Allows device control: false
Allows runtime control: false
Allows raw secrets: false
```

## Offline masked fixtures

The fixture provider creates masked state snapshots for:

```text
dummy-gree-ac-001
dummy-vrf-child-001
unknown-device
```

Allowed masked field names:

```text
Pow
Mod
SetTem
TemSen
WdSpd
SwUpDn
SwLfRig
Online
```

The fixtures contain only dummy/safe values and no real token, password, key, MAC, or account identifiers.

## Normalized state

The mapper produces a normalized bridge state with:

```text
DeviceId
IsKnownDevice
IsOnline
IsOn
Mode
TargetTemperatureC
CurrentTemperatureC
FanSpeed
SwingVertical
SwingHorizontal
UpdatedBy
RuntimeMode
SourceKind
Issues
```

Unknown devices, missing fields, unsupported fields, masked values, and stale/offline state are represented as controlled mapping issues. They do not cause unhandled exceptions.

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

Future live read adapters require a separate safety stage.
