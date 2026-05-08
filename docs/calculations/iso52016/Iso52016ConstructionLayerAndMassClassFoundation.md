# AE-ISO52016-CONSTRUCTION-001 ISO52016 Construction Layer and Mass Class Foundation

## Stage

- Stage id: `AE-ISO52016-CONSTRUCTION-001`
- Scope: pure construction-layer and mass-class engineering foundation for deterministic U-value, areal/effective heat capacity, and 5-node distribution descriptor outputs.

## Claim boundary

- ISO52016-inspired construction layer and mass class engineering foundation.
- Internal deterministic engineering anchors only.
- Compatibility envelope behavior preserved by default.
- No full ISO 52016 compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Why this foundation exists

- provide a typed construction assembly input contract for future envelope depth integration;
- compute deterministic U-value and capacity metrics from explicit layers;
- classify effective internal thermal mass with transparent thresholds;
- expose a simple 5-node descriptor as an internal anchor for future transient-path integration.

Current production envelope behavior remains unchanged by default. This stage does not replace the active `Iso52016RoomEnvelopeInputCalculator` compatibility path.

## Core formulas and assumptions

- Layer resistance:
  - massless layer with explicit thermal resistance uses that resistance;
  - otherwise `R_layer = thickness / conductivity`.
- Total resistance:
  - `R_total = Rsi + sum(R_layer) + Rse`.
- U-value:
  - `U = 1 / R_total`.
- Areal heat capacity:
  - `C_areal = sum(thickness * density * specificHeat)` for non-massless layers.
- Effective internal heat capacity:
  - deterministic resistance-threshold weighting from the internal side;
  - layers deeper than the penetration threshold are exponentially down-weighted.
- Mass class:
  - resolved from effective internal heat capacity via internal deterministic thresholds.
- Five-node descriptor:
  - internal surface, internal mass, core mass, external mass, external surface;
  - used as deterministic distribution metadata only.

## Limitations

- this is not a full ISO52016 transient conduction solver;
- this is not a claim of node-model equivalence with full standard implementations;
- descriptor nodes are governance-oriented engineering anchors, not production transient simulation state.

## Fixtures and tests

Deterministic fixtures are under:

- `tests/fixtures/iso52016/construction/light-external-wall-insulation-massless-layer.json`
- `tests/fixtures/iso52016/construction/medium-masonry-wall.json`
- `tests/fixtures/iso52016/construction/heavy-concrete-wall.json`
- `tests/fixtures/iso52016/construction/roof-lightweight-assembly.json`
- `tests/fixtures/iso52016/construction/ground-floor-slab-assembly.json`

Primary guard tests:

- `Iso52016ConstructionAssemblyCalculatorTests`
- `Iso52016ConstructionFixtureTests`
- `Iso52016ConstructionMassClassTests`
- `Iso52016ConstructionNodeDistributionTests`
- `Iso52016ConstructionTraceabilityTests`

## Future integration path

- optional application adapter can map domain envelope elements to construction assemblies;
- any production integration must remain controlled and compatibility-safe;
- default compatibility path remains authoritative until a dedicated opt-in integration stage is closed.
