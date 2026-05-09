# Thermal Zones and Adjacent Boundaries

## Purpose

This stage strengthens the internal C# thermal-zone and adjacent-boundary foundation for ISO52016-style hourly/matrix and multi-zone pipelines.

Scope includes deterministic handling for:
- conditioned zones;
- unconditioned adjacent zones;
- same-use adjacent zones;
- exterior boundaries;
- ground boundaries;
- adiabatic/internal boundaries;
- inter-zone conductance links.

## Supported zone kinds

- `Conditioned`
- `Unconditioned`
- `SameUseAdjacent`
- `External`
- `Ground`
- `Adiabatic`

## Supported boundary exposure kinds

- `ExteriorAir`
- `Ground`
- `AdjacentConditionedZone`
- `AdjacentUnconditionedZone`
- `SameUseAdjacentZone`
- `Adiabatic`
- `InternalMass`
- `Unknown` (only when explicitly allowed by validation mode)

## Supported boundary element kinds

- `Wall`
- `Roof`
- `Floor`
- `Window`
- `Door`
- `Slab`
- `InternalPartition`
- `ThermalBridge`
- `Generic`

## Adjacent unconditioned lane

`AdjacentUnconditionedZoneTemperatureCalculator` supports two deterministic modes:

1. `ReductionFactor` mode  
Convention: `T_adj = T_conditioned - b * (T_conditioned - T_exterior)`.

2. `DeterministicFallback` mode  
Weighted blend of conditioned and exterior temperature with explicit offset and diagnostics.

Fallback mode is never silent; assumptions and warnings are emitted.

## Same-use adjacent policy

Same-use adjacent boundaries are handled explicitly as simplified adiabatic-style or reduced-conductance paths based on policy inputs.

Default policy is adiabatic-style treatment:
- no artificial exterior-loss path;
- explicit diagnostics about applied assumption.

## Validation rules

Implemented deterministic checks include:
- missing adjacent zone;
- self-reference source/adjacent zone;
- exterior boundary with adjacent zone;
- adjacent boundary without required adjacent source;
- ground boundary without boundary-temperature lane;
- zero/non-positive area;
- zero/non-positive conductance for non-adiabatic links;
- duplicate/overlapping inter-zone pair links that can double-count;
- inconsistent opposing inter-zone area/conductance;
- transparent exterior metadata requirement when enabled.

Diagnostics are ordered deterministically for stable tests.

## Known limitations

- This stage is an internal engineering implementation layer.
- It is not a full building-domain authoring workflow.
- It does not claim full coupled airflow/moisture/HVAC plant modeling.

## Non-claims

- This is not a full ISO52016 compliance claim.
- This stage does not claim one-to-one equivalence with any py-building-energy lineage.
- This stage does not claim one-to-one equivalence with EnergyPlus outputs.
- This is an internal engineering calculation implementation with deterministic tests and validation anchors.
