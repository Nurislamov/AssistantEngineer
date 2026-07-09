# GREE-ALICE-46: VRF/GMV child-unit support

## Purpose

This document defines the offline VRF/GMV child-unit model for exposing indoor units as separate Alice/Yandex Smart Home devices.

## Current status

The model is offline fixture only. It does not use live Gree+ Cloud, MQTT, production wiring, credentials, real account identifiers, real device identifiers, MAC addresses, or command execution.

## Gateway model

The VRF/GMV gateway is a technical internal device:

```text
GatewayId: dummy-vrf-gateway-001
DisplayName: Demo VRF Gateway
HomeId: dummy-home-001
SystemName: Demo GMV System
GatewayKind: vrf-gateway
IsTechnicalDevice: true
ExposeToYandex: false
RuntimeMode: offline-fixture
```

The gateway is internal by default and is not published to Yandex `/devices` as a user-facing conditioner.

## Child-unit model

Indoor child units are modeled as room-bound user devices:

```text
ChildUnitId: dummy-vrf-child-living-001
StableYandexDeviceId: yandex-dummy-vrf-child-living-001
DisplayName: Кондиционер гостиная
RoomId: dummy-room-living-001
RoomName: Гостиная
ExposeToYandex: true
DeviceKind: vrf-child-indoor-unit

ChildUnitId: dummy-vrf-child-bedroom-001
StableYandexDeviceId: yandex-dummy-vrf-child-bedroom-001
DisplayName: Кондиционер спальня
RoomId: dummy-room-bedroom-001
RoomName: Спальня
ExposeToYandex: true
DeviceKind: vrf-child-indoor-unit
```

## Yandex exposure strategy

Gateway exposure is false by default.

Child units are exposed only when explicitly marked.

Unknown/internal devices are not exposed.

Only exposed devices appear in Yandex `/devices` mapping.

## Stable ID strategy

Yandex device IDs are stored stable IDs derived from offline-safe registry identifiers:

```text
yandex-dummy-vrf-child-living-001
yandex-dummy-vrf-child-bedroom-001
```

Changing display name or room name must not change the stable Yandex device ID.

Gateway IDs and child-unit Yandex IDs must not be confused.

Stable IDs must not contain MAC-like values, real account identifiers, real device identifiers, credentials, tokens, passwords, or device keys.

## Room binding

Each exposed child unit has a room binding:

```text
dummy-vrf-child-living-001 -> dummy-room-living-001 / Гостиная
dummy-vrf-child-bedroom-001 -> dummy-room-bedroom-001 / Спальня
```

## Query behavior

Querying a known exposed VRF child stable Yandex ID returns offline fixture state.

Querying an unknown child unit returns controlled offline unknown state.

Query does not call live Gree+ Cloud and does not use MQTT.

## Action behavior

Action for a known VRF child returns dry-run-fail-closed.

Action for an unknown VRF child returns controlled fail-closed response.

No action sends to Gree+ Cloud, MQTT, or a device.

## Out of scope

Live Gree+ Cloud integration, MQTT, live control, actual command execution, production runtime wiring, production deployment wiring, runtime secret configuration, migrations, PCAP, CSV, raw artifacts, real account identifiers, real device identifiers, MAC addresses, and credentials are out of scope.

## Safety boundaries

All data is offline fixture only.

Live Gree+ Cloud is not used.

MQTT is blocked.

Control remains fail-closed.

Production wiring is disabled.

Secrets are forbidden in the repository.

## Next stage

The next proposed stage is:

```text
GREE-ALICE-47 — add device registry import/admin boundary
```
