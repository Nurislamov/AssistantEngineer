# Thermal Zones Topology Foundation

## Purpose

`AE-ZONES-STANDARDS-001A` adds a canonical thermal-topology foundation that is calculation-ready for deterministic boundary-condition orchestration across rooms, zones, and surfaces.

This stage prepares shared topology modeling and diagnostics for future standard-inspired expansions.

## Supported boundary kinds

- `Outdoor`
- `Ground`
- `AdjacentConditionedZone`
- `AdjacentUnconditionedZone`
- `Adiabatic`
- `InternalPartition`
- `Other`

## What this prompt implements

- Topology build input contracts for zones, rooms, and surfaces.
- Canonical topology builder service with deterministic diagnostics.
- Topology validator service with identifier uniqueness and reference-integrity checks.
- Boundary-condition resolver for heat-transfer and temperature-source requirements.
- DI registration for reusable topology services.

## What this prompt intentionally does not implement

- No full coupled multizone solver.
- No full ISO52016 compliance claim.
- No EN16798 natural-ventilation formula expansion.
- No ISO13370 ground formula expansion.
- No ISO12831 DHW formula expansion.
- No EN15316 system-energy formula expansion.
- No external validation claim.

## Claim boundaries

- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

## Unlocked next work

- ISO13370 ground boundary expansion.
- EN16798 natural-ventilation expansion.
- ISO52016 boundary profile integration.
- Zone-level aggregation and boundary coupling orchestration.

Production adapter mapping from building-domain entities to `ThermalTopologyBuildInput` is intentionally deferred to `AE-ZONES-STANDARDS-001B`.
