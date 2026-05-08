# AE-BUI-VALIDATION-001 Building Input Validation And Correction Framework

## Stage

- Stage id: `AE-BUI-VALIDATION-001`
- Scope: deterministic governance validation of building input quality before HVAC/energy calculations.

## Claim boundary

- Building input validation and correction framework.
- Internal deterministic engineering governance only.
- No automatic production data mutation.
- No full ISO/EN compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Why this matters

- input-data quality problems can invalidate deterministic calculation outputs before any solver runs;
- pre-calculation validation provides transparent readiness status and corrective guidance;
- the framework protects ISO52016, ventilation, ground, DHW, and system-energy flows without changing physics.

## Validation dimensions

- categories:
  - Geometry
  - Envelope
  - Openings
  - BoundaryConditions
  - Ventilation
  - Ground
  - Dhw
  - SystemEnergy
  - Construction
  - Iso52016Readiness
  - DataCompleteness
  - Governance
- severities:
  - `Info`
  - `Warning`
  - `Error`
  - `Critical`
- readiness statuses:
  - `Ready`
  - `ReadyWithWarnings`
  - `BlockedByErrors`
  - `BlockedByCriticalErrors`
  - `NotEvaluated`

## Suggested corrections

- diagnostics may include `BuildingInputSuggestedCorrection` hints (`CorrectionId`, target path, proposed value, review flags);
- `IsAutomaticSafe` can be carried as metadata;
- this stage never mutates production data automatically.

## Current checks (deterministic minimum set)

- geometry consistency for building/floor/room availability and room area/height/volume;
- envelope and openings checks for positive dimensions, U-values, SHGC bounds, boundary orientation integrity, and facade-area sanity;
- ventilation ACH / heat-recovery bounds and missing-path warnings;
- ground-boundary metadata checks and fallback warnings;
- construction-layer readiness warnings for opt-in construction/mass scenarios;
- ISO52016 readiness hints for compatibility-only paths and fallback-heavy room inputs;
- DHW/system-energy completeness warnings where expected inputs are missing.

## Limitations

- this is not a solver and does not change any heat-balance equations;
- this stage does not apply automatic correction writes;
- this stage does not add API/frontend mutation workflows yet.

## Future path

- optional read-only API endpoint for validation result exposure;
- optional UI panel for diagnostics and suggested corrections;
- optional explicit user-approved correction workflow.
