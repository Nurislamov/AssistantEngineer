# ISO 52016 Matrix external validation annual anchors

This document records annual 8760 manual reference anchors for the ISO 52016 Matrix solver.

Validation anchors only, not full parity.

## Scope

These anchors are intentionally narrow. They check deterministic Matrix solver behaviour against independent manual formulas and fixture summaries.

They are allowed to use pyBuildingEnergy-style and EnergyPlus-style naming only to make future comparison work easy to recognize.

## Explicit non-claims

- No exact pyBuildingEnergy numerical parity claim.
- No exact EnergyPlus numerical parity claim.
- No ASHRAE 140 validation coverage claim.
- No full annual building simulation parity claim.
- No weather-file parity claim.
- No coupled multi-zone parity claim.

## Annual 8760 reference

The first annual reference is `manual-independent-annual-8760-seasonal-loads.json`.

It is a manual 8760 anchor with predictable seasonal loads. It is not a pyBuildingEnergy or EnergyPlus parity fixture.