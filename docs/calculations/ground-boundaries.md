# Ground Boundaries (Internal Engineering Foundation)

## Purpose

This module provides an internal engineering implementation for ground-contact boundaries used in thermal topology and ISO52016/MultiZone hourly pipelines.

It is deterministic, fixture-driven, and intended as a validation-anchor lane only.

## Supported ground boundary types

- `SlabOnGround`
- `SuspendedFloor`
- `HeatedBasementWall`
- `HeatedBasementFloor`
- `UnheatedBasementCeiling`
- `GenericGroundContact`
- `Unsupported` (explicit fallback path with diagnostics)

## Ground temperature profile modes

- `ConstantAnnualMean`
  - Ground temperature profile is constant and equal to configured annual mean ground temperature.
  - Deterministic assumptions/diagnostics are emitted explicitly.
- `SeasonalSinusoidal`
  - Ground profile is generated as deterministic annual sinusoid around configured annual mean.
  - Inputs: annual mean, amplitude, phase shift.
  - Phase-shift convention: coldest point is anchored at configured `phaseShiftDays`.

## Simplified heat-transfer convention

- Generic lane: `H_ground = Area * U`
- Heat-flow sign convention:
  - `Q_ground = H_ground * (T_ground - T_zone)`
  - Positive `Q_ground`: ground-to-zone heat gain.
  - Negative `Q_ground`: zone-to-ground heat loss.

### Simplified slab lane

- Uses generic `Area * U` baseline.
- Optional deterministic perimeter/characteristic-dimension correction when available.
- If shape metadata is incomplete, fallback to generic lane with diagnostics.

### Simplified basement lane

- Supports heated/unheated basement-style floor/wall contacts through deterministic below-grade factors.
- If required depth/height metadata is incomplete, fallback to generic lane with diagnostics.

## Thermal topology integration

- Ground boundaries are expected as `BoundaryExposureKind.Ground`.
- Ground boundaries must not specify adjacent zone ids unless an explicit virtual ground-zone architecture is introduced.
- Ground boundaries are not treated as exterior-air boundaries.
- Ground boundaries are linked by boundary id to thermal-topology boundaries and can carry per-boundary temperature profiles.

## ISO52016 / MultiZone integration

- Ground boundary profiles are mapped into MultiZone boundary-temperature lanes.
- When explicit ground profile is missing, fallback is deterministic constant ground temperature derived from exterior annual mean (not direct exterior boundary-lane reuse).
- Ground boundary diagnostics explicitly indicate ground-temperature lane usage.
- Ground boundaries are validated separately from exterior boundaries to avoid accidental double counting.

## Validation rules

- Ground boundary with adjacent zone id is rejected.
- Ground boundary with zero/non-positive area is rejected.
- Ground boundary with negative/non-positive U-value is rejected.
- Missing ground parameters for selected simplified mode emit diagnostics/fallbacks.
- Invalid soil conductivity is rejected.
- Invalid amplitude is rejected.
- Invalid phase shift outside accepted range is rejected.
- Profile length mismatch is rejected.
- Ground boundary cannot be treated as exterior boundary in topology/multi-zone validation.
- Natural ventilation opening on ground boundary is rejected by ventilation topology validator.

## Deterministic fixtures

- `tests/fixtures/ground/foundation/constant-annual-mean-ground-boundary.json`
- `tests/fixtures/ground/foundation/seasonal-sinusoidal-ground-profile.json`
- `tests/fixtures/ground/foundation/ground-vs-exterior-comparison.json`
- `tests/fixtures/ground/foundation/slab-on-ground-simplified-fallback.json`
- `tests/fixtures/ground/foundation/basement-simplified-case.json`
- `tests/fixtures/iso52016/multi-zone-invalid/invalid-ground-topology.json`

## Known limitations

- Simplified engineering implementation only.
- No detailed 2D/3D soil simulation.
- No full catalogue of all ground-contact cases.
- No moisture coupling model.
- No groundwater model.
- No detailed perimeter-insulation model beyond implemented simplified factors.
- No full standard compliance claim.
- No one-to-one external comparison equivalence claim.
