# ISO 52016 Matrix external validation naming anchors

This page documents external-style naming anchors for the ISO 52016 Matrix solver validation layer.

Validation anchors only, not full parity.

## Purpose

The naming anchors reserve stable names for future external comparison work while keeping the engineering claims honest.

The fixture set may use names that are familiar from:

- pyBuildingEnergy-style hourly heating/cooling need references;
- EnergyPlus-style zone ideal-loads design-day references.

These are naming and traceability anchors only.

## Explicit non-claims

- No exact pyBuildingEnergy numerical parity claim.
- No exact EnergyPlus numerical parity claim.
- No ASHRAE 140 validation coverage claim.
- No ExternalParityCovered claim.
- No full annual building simulation parity claim.

## Fixture families

- `PyBuildingEnergyStyleNamesOnly`
- `EnergyPlusStyleNamesOnly`

Every fixture must keep `scope = ValidationAnchorOnly`.