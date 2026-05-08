# Standards Foundation

## Purpose

`AE-STANDARDS-FOUNDATION-001` introduces a shared foundation for standard-inspired calculation lanes without implementing full standard formula expansions in this stage.

This foundation is shared by:
- Thermal zones / adjacent boundaries
- EN16798-style natural ventilation
- ISO13370-inspired ground boundary lane
- ISO12831-3-inspired domestic hot water lane
- EN15316-inspired system energy lane

## Scope boundary

This stage is limited to calculation contracts, deterministic helpers, diagnostics, and disclosure structure.

Explicit non-claims:
- No full ISO compliance claim.
- No full EN compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.

## What this foundation provides

- Shared standard disclosure contracts and claim-boundary model.
- Shared standard diagnostics payload shape.
- Shared engineering units and deterministic unit conversion helpers.
- Shared annual profile shape validation contracts and service (8760/12/24 + finite/non-negative checks).
- Shared thermal boundary/topology contracts for future multi-zone and boundary lanes.

## What this foundation intentionally does not do

- It does not implement full thermal-zone formulas.
- It does not implement full EN16798 natural-ventilation formulas.
- It does not implement full ISO13370 ground formulas.
- It does not implement full ISO12831-3 domestic-hot-water formulas.
- It does not implement full EN15316 system-energy chain formulas.

Those expansions are unlocked by the next prompt series and will build on these shared contracts.
