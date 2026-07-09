# GREE-ALICE-35: offline account and device registry boundary

## Purpose

This stage adds an offline-only registry boundary for future bridge account, home, room, split AC, VRF/GMV gateway, VRF/GMV child indoor unit, and Yandex Smart Home exposed device modeling.

The registry is not a production registry. It is a static fixture boundary under:

```text
src/Integrations/GreeAliceBridge
```

## Fixture registry

The offline registry uses dummy-only values:

```text
Account: dummy-account-001
Home: dummy-home-001
Room: dummy-room-001
Split AC: dummy-gree-ac-001
VRF/GMV gateway: dummy-vrf-gateway-001
VRF/GMV child indoor unit: dummy-vrf-child-001
```

The VRF/GMV child indoor unit references the dummy gateway:

```text
dummy-vrf-child-001 -> dummy-vrf-gateway-001
```

## Device kinds

The registry boundary explicitly supports future device kinds:

```text
split-ac
vrf-gateway
vrf-child-indoor-unit
unknown
```

Current Yandex Smart Home `/devices` output remains limited to the existing offline split AC fixture, `dummy-gree-ac-001`. The registry already models future VRF/GMV records without enabling live discovery or control.

## Safety boundary

```text
Uses real Gree+ Cloud data: false
Uses real account identifiers: false
Uses real device identifiers: false
Allows runtime control: false
Allows device control: false
Allows MQTT CONNECT: false
Allows MQTT SUBSCRIBE: false
Allows MQTT PUBLISH: false
Registry mode: offline-fixture-registry
```

## Explicitly not implemented

```text
No live Gree+ Cloud data.
No real account identifiers.
No real device identifiers.
No MQTT CONNECT.
No MQTT SUBSCRIBE.
No MQTT PUBLISH.
No device control.
No production runtime wiring.
No deployment changes.
No migrations.
```
