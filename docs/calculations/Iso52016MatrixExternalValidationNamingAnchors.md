# ISO 52016 Matrix external validation naming anchors

This page documents external-style naming anchors for the ISO 52016 Matrix solver validation layer.

Validation anchors only, not full equivalence claim.

## Purpose

The naming anchors reserve stable names for future external comparison work while keeping the engineering claims honest.

The fixture set may use names that are familiar from:

- StandardReference-style hourly heating/cooling need references;
- EnergyPlus-style zone ideal-loads design-day references.

These are naming and traceability anchors only.

## Explicit non-claims

- No exact StandardReference numerical equivalence claim.
- No exact EnergyPlus numerical equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor coverage claim.
- No ExternalReferenceCovered claim.
- No full annual building simulation equivalence claim.

## Fixture families

- `StandardReferenceStyleNamesOnly`
- `EnergyPlusStyleNamesOnly`

Every fixture must keep `scope = ValidationAnchorOnly`.