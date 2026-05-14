# Calculations -> Buildings.Domain Decoupling Plan (Snapshot/Projection Boundary)

## Scope
- Date: 2026-05-14.
- Goal of this step: inventory + staged snapshot/projection plan + baseline guard.
- Non-goals for this step:
- No calculation physics changes.
- No API route changes.
- No DTO shape changes (for now).
- No mass replacement of `Room`/`Floor`/`Building` in pipeline.
- No EF/repository rewrites.

## Current dependency map baseline

### Production (`src/Backend/AssistantEngineer.Modules.Calculations/**`)
- Total files referencing `AssistantEngineer.Modules.Buildings.Domain`: `138`.
- Category split:
- `public contracts`: `18`
- `application services`: `96`
- `calculators`: `17`
- `mappers/adapters`: `7`

Machine-readable full baseline list:
- `tests/fixtures/architecture/calculations-buildings-domain-reference-allowlist.txt`

### Tests (`tests/AssistantEngineer.Tests/**`)
- Total test files referencing `AssistantEngineer.Modules.Buildings.Domain`: `65`.
- Category split:
- `Calculations`: `49`
- `Reporting`: `2`
- `Api`: `1`
- `Architecture`: `1`
- `Benchmarks`: `3`
- `Buildings`: `7`
- `Validation`: `2`

This test-side coupling is expected for current fixtures/integration coverage and is not changed in this step.

## Category inventory (production)

### Public contracts (18)
- Full list is maintained in the baseline allowlist.
- Most of this debt is in:
- `Application/Contracts/Iso52016/*`
- `Application/Contracts/Weather*/*`
- `Application/Contracts/Validation/BuildingInput/*`

### Application services (96)
- Dominant areas:
- `Application/Services/Pipeline/*`
- `Application/Services/ReferenceData/*`
- `Application/Services/Validation/BuildingInput/*`
- `Application/Services/Iso52016/*`
- Also includes `Application/Abstractions/*` that currently expose `Buildings.Domain` types.

### Calculators (17)
- Dominant areas:
- `Application/Services/CoolingLoads/*`
- `Application/Services/HeatingLoads/*`
- `Application/Services/Iso52016/*`
- `Application/Services/Ventilation/*`

### Mappers/adapters (7)
- Dominant areas:
- `Application/Mappers/CalculationsContractEnumMapper.cs`
- `Application/Services/*Adapter*.cs`
- `Application/Services/Pipeline/*Mapper*.cs`

## Target boundary snapshots

The decoupling target is staged replacement of direct `Buildings.Domain` inputs with calculation snapshots/projections.

### `BuildingCalculationInputSnapshot`
- `BuildingId`, `ProjectId`, `BuildingName`
- climate reference (`ClimateInputSnapshot`)
- floors (`FloorCalculationInputSnapshot[]`)
- optional thermal-zones/system metadata needed by calculation paths

### `FloorCalculationInputSnapshot`
- `FloorId`, `BuildingId`, `FloorName`
- `RoomCalculationInputSnapshot[]`

### `RoomCalculationInputSnapshot`
- Status: introduced (2026-05-14) in `AssistantEngineer.Modules.Buildings.Application.Contracts.Calculations`.
- `RoomId`, `FloorId`, `BuildingId`, `ProjectId`, `RoomName`
- geometry and load inputs (`AreaM2`, `HeightM`, `VolumeM3`, temperatures, people/equipment/lighting)
- envelope (`RoomEnvelopeElementSnapshot[]`, `RoomCalculationWindowSnapshot[]`, `RoomCalculationWallSnapshot[]`)
- ventilation (`RoomCalculationVentilationInputSnapshot`)
- optional schedules/profile references (`Occupancy`, `Equipment`, `Lighting`)
- climate/ground references where available (`RoomCalculationClimateInputSnapshot`, `RoomCalculationGroundBoundaryInputSnapshot`)
- deterministic mapping path is available via `RoomCalculationInputSnapshotMapper` (ordered wall/window mapping by identifier)

### `EnvelopeElementSnapshot`
- stable envelope element abstraction for transmission/ground paths
- fields required for area/U-value/boundary-kind/orientation and adjacency metadata

### `WindowSnapshot`
- area, U-value, SHGC, orientation, shading flags/properties used by solar and cooling paths

### `WallSnapshot`
- area, U-value, boundary kind, orientation, adjacency/ground contact metadata

### `VentilationInputSnapshot`
- ACH/mechanical airflow/recovery/infiltration/controls fields consumed by ventilation paths

### `ClimateInputSnapshot`
- design temperatures and optional hourly climate references
- normalization flags/quality markers needed by weather/solar/ISO 52016 paths

### `GroundBoundaryInputSnapshot`
- slab/wall/burial/perimeter/insulation parameters needed by ISO 13370 + simplified ground paths

## Staged migration sequence

1. Freeze baseline with allowlist guard (done in this step).
2. Start with `public contracts` debt (smallest controlled set: 18 files).
3. Introduce snapshot models under a stable application boundary contract location.
- Progress: `RoomCalculationInputSnapshot` introduced in `Buildings.Application.Contracts.Calculations`.
4. Add adapters in `Buildings` (or shared application boundary layer) that project domain entities to snapshots.
- Progress: `RoomCalculationInputSnapshotMapper` introduced in `Buildings.Application.Mappers`.
5. Switch `pipeline` and `validation` entry seams to consume snapshots at boundaries while keeping internal calculators unchanged.
- Progress: room load pipeline now constructs and consumes room snapshot primitives for climate/geometry/internal gains/ventilation inputs, while transmission/solar still consume domain entities.
6. Migrate calculators incrementally from direct domain types to snapshots (one subsystem at a time).
7. Retire allowlist entries continuously as references are removed.

## Baseline architecture guard

Guard added:
- `tests/AssistantEngineer.Tests/Architecture/CalculationsBuildingsDomainReferenceBaselineGuardTests.cs`

Guard policy:
- Scan all `src/Backend/AssistantEngineer.Modules.Calculations/**/*.cs`.
- Collect files that reference `AssistantEngineer.Modules.Buildings.Domain`.
- Fail on any new file not in baseline allowlist.
- Fail on stale allowlist entries (keeps baseline list honest while debt is reduced).

Allowlist source:
- `tests/fixtures/architecture/calculations-buildings-domain-reference-allowlist.txt`

## Recommended first migration target

First target:
- `public contracts` subset in `Application/Contracts/Weather*` and `Application/Contracts/Iso52016` that currently expose `Buildings.Domain` enums/entities.

Why first:
- Smallest bounded set.
- Highest boundary value (contracts are boundary-facing).
- Lowest risk to numerical physics (representation boundary first, algorithms unchanged).
