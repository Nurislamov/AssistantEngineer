# Device Registry Import/Admin Boundary

## Purpose

This stage adds an offline-only registry import/admin boundary for the future process that will add air conditioners to the bridge registry used by Alice.

Yandex devices come from our bridge registry, not direct Alice discovery and not direct Gree Cloud discovery.

## Current status

The boundary exists as contracts, an offline template provider, and an offline validator.

Real imports are not enabled. Admin UI is not implemented. No admin endpoints are exposed.

## Out of scope

This stage does not add live Gree+ Cloud integration, MQTT, device control, production wiring, production deployment changes, migrations, runtime configuration, or real import data.

## How devices will be added to Alice

```text
manual/import draft
        ↓
validation
        ↓
approved registry snapshot
        ↓
Yandex /devices
```

Gree Cloud discovery must not auto-expose devices. Any discovered device in a future stage must pass manual review before it can be shown to Alice.

## Registry import flow

The import boundary accepts draft-shaped data only in offline-template mode today.

Every draft record must be masked dummy/template data. Validation blocks real-looking identifiers, hardware-like identifiers, and sensitive material.

## Manual review requirement

Manual review is required before exposure. Review must decide device scope, room binding, and whether `ExposeToYandex` is allowed.

Auto-exposure of discovered devices is disabled.

## Import draft model

The draft model contains:

```text
Account
Home
Rooms
Split AC devices
VRF/GMV gateways
VRF/GMV child units
Exposure decisions
Stable Yandex IDs
```

## Split AC import model

Split AC records include an import device id, device kind, display name, room id, stable Yandex device id, exposure flag, masked flag, and dummy/template flag.

An exposed split AC must have a stable Yandex device id and a valid room binding.

## VRF/GMV Gateway Import Model

Gateway records include an import gateway id, home id, system name, display name, exposure flag, technical-device flag, masked flag, and dummy/template flag.

Gateway is internal by default. It is not exposed to Alice as a user device in this stage.

## VRF/GMV Child-Unit Import Model

Child-unit records include an import child-unit id, parent gateway id, display name, room id, stable Yandex device id, exposure flag, optional indoor unit details, masked flag, and dummy/template flag.

Child units can be exposed as Yandex user devices after explicit review when stable ID and room binding rules pass.

## Stable Yandex ID Rules

Stable Yandex IDs are required for exposed devices.

Stable Yandex IDs must remain stable. Room/name changes must not change stable IDs.

Stable IDs must not be derived from hardware-like values or real cloud identifiers.

## Room Binding Rules

Room binding is required for exposed devices.

Room ids must reference rooms in the same draft. Room/name changes must not change stable IDs.

## Exposure Policy

`ExposeToYandex = true` is allowed only after explicit review.

Gateway exposure is false by default. Unknown/internal devices are not exposed. Gree Cloud discovery does not auto-expose devices.

## Validation Rules

Validation checks:

```text
offline-template mode only
masked dummy/template records only
stable Yandex ID required for exposed devices
room binding required for exposed devices
duplicate stable Yandex IDs rejected
duplicate import IDs rejected
unknown parent gateway rejected
unknown room binding rejected
gateway exposure rejected
hardware-like identifiers rejected
real-looking account/device identifiers rejected
sensitive material rejected
```

## Forbidden Data

No real credentials/secrets/account IDs/device IDs in repo.

No real tokens, passwords, device keys, PCAP, CSV real data artifacts, runtime env config, or production import artifacts are added by this stage.

## Future Admin/Import Paths

Future live discovery may produce a draft, but it must not publish directly to `/devices`.

A future admin path must keep review/approval separate from discovery, and must assign stable Yandex IDs, room bindings, and device scope before registry exposure.

## Next Stage

GREE-ALICE-48 should add the Yandex account linking boundary. It should not enable live Gree+ Cloud integration, MQTT, device control, or production wiring.
