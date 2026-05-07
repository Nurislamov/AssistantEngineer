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

Production adapter mapping from building-domain entities to `ThermalTopologyBuildInput` remains intentionally deferred to a dedicated follow-up prompt.

## AE-ZONES-STANDARDS-001B - Boundary calculation integration

### Added in this stage

- Deterministic zone-level boundary aggregation for rooms, zones, and building totals.
- Heat-transfer coefficient integration using `H = A * U` for active heat-transfer boundaries with valid area and U-value.
- Boundary classification and preparation for:
  - outdoor
  - ground
  - adjacent conditioned
  - adjacent unconditioned
  - internal partition
  - adiabatic
  - other
- Deterministic boundary-temperature source resolution from:
  - outdoor temperature input
  - ground temperature input
  - adjacent zone temperatures
  - adjacent unconditioned temperature map
- Diagnostics and disclosure carry-through for unresolved or incomplete boundary inputs.

### Scope boundary for this stage

- No full coupled multizone simulation.
- No full ISO52016 compliance claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

### Unlocks

- ISO13370 ground boundary integration (`AE-GROUND-ISO13370-001A`).
- EN16798 natural-ventilation zone integration (`AE-VENT-EN16798-001A`).
- Future ISO52016 boundary coupling refinement and profile mapping.

### Integration note

A direct mapper from boundary-calculation outputs to ISO52016 physical room-model inputs is intentionally deferred because current ISO52016 physical contracts require construction-layer and model-node details not provided by this topology-level deterministic integration stage.
