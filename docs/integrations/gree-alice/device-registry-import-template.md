# Device Registry Import Template

This document describes the offline import template/schema. It is not a CSV file and not a real data artifact.

All examples use dummy/template values only.

## Account Section Fields

```text
ImportAccountId: dummy-import-account-001
DisplayName: Dummy import account
IsMasked: true
IsDummyOrTemplate: true
```

## Home Section Fields

```text
ImportHomeId: dummy-import-home-001
DisplayName: Dummy import home
IsMasked: true
IsDummyOrTemplate: true
```

## Room Section Fields

```text
ImportRoomId: dummy-import-room-living-001
DisplayName: Гостиная
HomeId: dummy-import-home-001
IsMasked: true
IsDummyOrTemplate: true
```

```text
ImportRoomId: dummy-import-room-bedroom-001
DisplayName: Спальня
HomeId: dummy-import-home-001
IsMasked: true
IsDummyOrTemplate: true
```

## Split AC Section Fields

```text
ImportDeviceId: dummy-import-split-ac-001
DeviceKind: split-ac
DisplayName: Dummy split AC
RoomId: dummy-import-room-living-001
StableYandexDeviceId: yandex-dummy-import-split-ac-001
ExposeToYandex: true
IsMasked: true
IsDummyOrTemplate: true
```

## VRF Gateway Section Fields

```text
ImportGatewayId: dummy-import-vrf-gateway-001
HomeId: dummy-import-home-001
SystemName: Dummy GMV import system
DisplayName: Dummy VRF import gateway
ExposeToYandex: false
IsTechnicalDevice: true
IsMasked: true
IsDummyOrTemplate: true
```

## VRF Child Unit Section Fields

```text
ImportChildUnitId: dummy-import-vrf-child-living-001
ParentGatewayId: dummy-import-vrf-gateway-001
DisplayName: Кондиционер гостиная
RoomId: dummy-import-room-living-001
StableYandexDeviceId: yandex-dummy-import-vrf-child-living-001
ExposeToYandex: true
IndoorUnitAddress: null
IndoorUnitModel: null
CapacityKw: null
IsMasked: true
IsDummyOrTemplate: true
```

```text
ImportChildUnitId: dummy-import-vrf-child-bedroom-001
ParentGatewayId: dummy-import-vrf-gateway-001
DisplayName: Кондиционер спальня
RoomId: dummy-import-room-bedroom-001
StableYandexDeviceId: yandex-dummy-import-vrf-child-bedroom-001
ExposeToYandex: true
IndoorUnitAddress: null
IndoorUnitModel: null
CapacityKw: null
IsMasked: true
IsDummyOrTemplate: true
```

## Exposure Section Fields

```text
ImportObjectId: dummy-import-split-ac-001
ObjectKind: split-ac
ExposeToYandex: true
Reviewed: true
StableYandexDeviceId: yandex-dummy-import-split-ac-001
RoomId: dummy-import-room-living-001
```

```text
ImportObjectId: dummy-import-vrf-gateway-001
ObjectKind: vrf-gateway
ExposeToYandex: false
Reviewed: true
StableYandexDeviceId: null
RoomId: null
```

## Validation Expectations

Validation expects offline-template mode and masked dummy/template data.

Exposed devices require stable Yandex IDs and room binding.

VRF child units require an existing parent gateway.

Discovery must not auto-expose devices.

No real credentials/secrets/account/device IDs, tokens, passwords, device keys, hardware-like identifiers, live Gree+ Cloud data, MQTT data, control data, or production wiring data may appear in the repository.
