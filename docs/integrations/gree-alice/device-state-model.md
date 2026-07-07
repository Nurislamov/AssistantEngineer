# GREE-ALICE device and state model

## Stage

`GREE-ALICE-04` defines the internal cloud device and state model for the Gree+ Cloud probe.

This stage does not add control commands, Yandex Smart Home endpoints, production deployment, runtime database changes, Telegram changes, or migrations.

## Confirmed project facts

The first real cloud validation confirmed:

```text
Region: Ouzbekistan
REST server: https://hkgrih.gree.com
Login: succeeded
Homes: 1
Rooms: 1
Devices: 1
First device classification: cloud-ac-candidate
```

A private traffic capture from the GREE+ mobile app confirmed these network paths:

```text
hkgrih.gree.com:443      REST / cloud API
mqtt-hk.gree.com:1994    MQTT / live cloud channel candidate
255.255.255.255:7000     local Wi-Fi discovery fallback
```

The CSV capture itself is private diagnostic material and must not be committed.

## Device classification

The probe uses conservative classifications:

| Classification | Meaning |
|---|---|
| `cloud-ac-candidate` | A Gree cloud device with enough fields for future AC bridge work, but without proof that it is split-only or VRF parent/child. |
| `split-candidate` | A future stricter split AC classification if model/type fields confirm it. |
| `vrf-gateway-candidate` | A future VRF gateway classification if model/type/raw fields prove gateway behavior. |
| `vrf-child-unit-candidate` | A future VRF child-unit classification if parent/child relationships are proven by meaningful cloud fields. |
| `unknown` | Device is visible but not enough safe fields are known for bridge mapping. |

Do not classify a device as VRF only because masked values like `<not set>` are present.

## Safe device snapshot

The internal safe device snapshot keeps fields that are useful for bridge decisions:

```text
HomeId
HomeName
RoomId
RoomName
DeviceId
DeviceName
DeviceType
DeviceModel
ProductModel
Brand
Vendor
Mid
Hid
Version
Online
Classification
LocalKeyProvided
LocalKeyMasked
MacMasked
ParentMacMasked
ChildMacMasked
RawFieldNames
SafeRawProperties
```

Full secret values are not part of the safe model.

## Secret fields

These fields are treated as secret or sensitive by default:

```text
password
token
key
mac
pmac
cmac
barCode
latitude
longitude
ssid
```

The probe may record whether a value exists, but must not write the full value to console output, docs, committed files, or default artifacts.

## Initial observed safe fields

The first observed device included safe signals such as:

```text
brand: gree
mid: 10001
hid: contains U-WB05 module signature
ver: V3.4.M
key: provided, masked
mac: provided, masked
```

This is currently enough for `cloud-ac-candidate`, not enough for verified VRF classification.

## State snapshot

The future state snapshot should be normalized into:

```text
Power
Mode
SetpointCelsius
IndoorTemperatureCelsius
FanSpeed
SwingVertical
SwingHorizontal
ObservedAtUtc
SafeRawState
```

If the cloud response does not contain these fields yet, the state snapshot should be empty but valid.

## Next stages

Recommended next stages:

```text
GREE-ALICE-05 — Read current device state
GREE-ALICE-06 — Command model / power and setpoint probe
GREE-ALICE-07 — Yandex Smart Home bridge skeleton
```
