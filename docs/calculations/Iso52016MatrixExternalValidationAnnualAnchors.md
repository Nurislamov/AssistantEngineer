# ISO 52016 Matrix external validation annual anchors

This document records annual 8760 manual reference anchors for the ISO 52016 Matrix solver.

Validation anchors only, not full equivalence claim.

## Scope

These anchors are intentionally narrow. They check deterministic Matrix solver behaviour against independent manual formulas and fixture summaries.

They are allowed to use StandardReference-style and EnergyPlus-style naming only to make future comparison work easy to recognize.

## Explicit non-claims

- No exact StandardReference numerical equivalence claim.
- No exact EnergyPlus numerical equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor coverage claim.
- No full annual building simulation equivalence claim.
- No weather-file equivalence claim.
- No coupled multi-zone equivalence claim.

## Annual 8760 reference

The first annual reference is `manual-independent-annual-8760-seasonal-loads.json`.

It is a manual 8760 anchor with predictable seasonal loads. It is not a StandardReference or EnergyPlus comparison workflow fixture.