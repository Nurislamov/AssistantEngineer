# ISO52016 Decomposition Review (P9-01 / P9-01A / P9-01B / P9-01B1)

## Purpose

Capture a review-only decomposition map for the ISO52016 solver/service surface to reduce architectural risk in future refactors while preserving current calculation behavior.

## Scope

This review covers ISO52016-related calculation services, matrix/multi-zone/physical pipelines, validation fixtures, verification tooling, and workflow integration touchpoints.

## Non-claims

- No calculation physics change claim.
- No expected value change claim.
- No EnergyPlus parity claim.
- No pyBuildingEnergy full parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.
- No production security certification claim.

## Current ISO52016 surface

- Application entry and orchestration:
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Iso52016BuildingEnergySimulationApplicationService.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Iso52016BuildingDomainSimulationFacade.cs`
  - `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Iso52016BuildingEnergyCalculator.cs`
- Hourly and monthly calculation services:
  - `.../Iso52016HourlyHeatBalanceCalculator.cs`
  - `.../Iso52016MonthlyQuasiSteadyStateCalculator.cs`
- Matrix chain:
  - `.../Matrix/Iso52016MatrixReducedRoomModelBuilder.cs`
  - `.../Matrix/Iso52016MatrixHourlySolver.cs`
  - `.../Matrix/Iso52016MatrixRoomEnergySimulationService.cs`
  - `.../Matrix/Iso52016MatrixRoomEnergySimulationResultMapper.cs`
- Multi-zone chain:
  - `.../MultiZone/Iso52016MultiZoneNormalizedInputBuilder.cs`
  - `.../MultiZone/Iso52016MultiZoneInputValidator.cs`
  - `.../MultiZone/Iso52016MultiZoneHourlySolver.cs`
  - `.../MultiZone/Iso52016MultiZoneEnergySimulationService.cs`
- Weather/solar/gains:
  - `.../Iso52016WeatherSolarContextBuilder.cs`
  - `.../Iso52016RoomSolarGainProfileBuilder.cs`
  - `.../Iso52016RoomInternalGainProfileBuilder.cs`
  - `.../Iso52016RoomHourlyInputProfileBuilder.cs`

## Component map

Canonical mapping for P9-01 is maintained in:

- `docs/validation/iso52016-component-map.md`
- `docs/validation/iso52016-component-map.json`
- `docs/validation/iso52016-behavior-characterization-inventory.md`
- `docs/validation/iso52016-behavior-characterization-inventory.json`
- `docs/validation/iso52016-matrix-solver-seam-design.md`
- `docs/validation/iso52016-matrix-solver-seam-design.json`
- `docs/validation/iso52016-matrix-solver-seam-risk-register.md`
- `docs/validation/iso52016-matrix-solver-seam-risk-register.json`
- `docs/validation/iso52016-matrix-solver-characterization-hardening.md`
- `docs/validation/iso52016-matrix-solver-characterization-hardening.json`

## Responsibility boundaries

- Physics model and balance calculations should remain isolated from API/report DTO mapping.
- Matrix assembly and matrix solve should be distinct seams for test locking and future extraction.
- Weather/solar/gains preparation should stay as input-pipeline services, not mixed into report mapping.
- Validation/provenance artifacts should annotate evidence strength, not imply formal validation parity/certification.

## Hotspots

- `Iso52016HourlyHeatBalanceCalculator` (515 LOC) mixes multiple responsibilities (normalization, internal gains, solar gains, ventilation/ground coupling, load decision, diagnostics assembly).
- `Iso52016MatrixHourlySolver` (490 LOC) combines matrix assembly concerns, HVAC control policy, solve loop, and monthly summary aggregation.
- `Iso52016MultiZoneNormalizedInputBuilder` (455 LOC) combines topology classification integration, profile normalization, boundary-link construction, and diagnostics policy.
- `Iso52016MultiZoneInputValidator` (442 LOC) contains dense rule families that can be split into zone/profile/boundary/inter-zone validators.
- Duplication seam: `Iso52016RoomEnergySimulationService` and `Iso52016MatrixRoomEnergySimulationService` share a near-identical pre-solver pipeline.

## Decomposition candidates

- Extract matrix coefficient/boundary assembly seam from `Iso52016MatrixHourlySolver`.
- Split hourly heat-balance preprocessing and result composition seam from `Iso52016HourlyHeatBalanceCalculator`.
- Split multi-zone input validation rule sets into focused validators.
- Converge duplicated room simulation pre-solver pipeline via shared internal orchestration seam.
- Keep behavior locked by existing ISO52016 fixture and matrix anchor tests before any refactor stage.

## Behavior-locking tests

Representative locks already present:

- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixHourlySolverTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixExternalValidationFixtureTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixExternalValidationAnnualAnchorTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Iso52016RoomEnergySimulationServiceTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/MultiZone/Iso52016MultiZoneHourlySolverTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/Iso52016/Physical/Iso52016PhysicalRoomEnergySimulationServiceTests.cs`

## Validation/provenance relationship

- P9-03 provenance inventory remains canonical for evidence strength and placeholder separation:
  - `docs/validation/validation-fixture-provenance-model.md`
  - `docs/validation/validation-fixture-provenance-inventory.md`
- P9-01 adds component-level traceability links, but does not promote maturity claims or formal validation status.

## Proposed staged backlog

- `P9-01A` ISO52016 behavior characterization inventory (`TestOnly`) - implemented with explicit component coverage and retained-gap classification.
- `P9-01B` Matrix assembly/solver seam extraction design (`DesignOnly`) - implemented.
- `P9-01B1` Matrix/solver seam characterization hardening (`TestOnly`) - implemented.
- `P9-01C` Report/diagnostics mapping seam review (`AuditOnly`/`TestOnly`).
- `P9-01D` Weather/solar/gains input pipeline seam review (`AuditOnly`/`TestOnly`).
- `P9-01E` ISO52016 naming/ubiquitous language cleanup candidates (`DocsOnly`/`RefactorOnly` with approval).
- `P9-01F` Fixture traceability to ISO52016 components (`DocsOnly`/`TestOnly`).

## P9-01A characterization status

- Stage status: `Implemented` (test/inventory only).
- Added characterization inventory artifacts:
  - `docs/validation/iso52016-behavior-characterization-inventory.md`
  - `docs/validation/iso52016-behavior-characterization-inventory.json`
  - `docs/validation/iso52016-behavior-characterization-inventory.schema.json`
- Added focused seam tests where deterministic setup is safe:
  - `Iso52016MatrixAssemblyCharacterizationTests`
  - `Iso52016SolverOutputCharacterizationTests`
  - `Iso52016AggregationCharacterizationTests`
  - `Iso52016ReportMappingCharacterizationTests`
- No formula, expected-value fixture, runtime, or API behavior changes are claimed.

## P9-01B seam design status

- Stage status: `Implemented` (`DesignOnly`).
- Added seam-design artifacts:
  - `docs/validation/iso52016-matrix-solver-seam-design.md`
  - `docs/validation/iso52016-matrix-solver-seam-design.json`
  - `docs/validation/iso52016-matrix-solver-seam-design.schema.json`
- Added seam-risk artifacts:
  - `docs/validation/iso52016-matrix-solver-seam-risk-register.md`
  - `docs/validation/iso52016-matrix-solver-seam-risk-register.json`
  - `docs/validation/iso52016-matrix-solver-seam-risk-register.schema.json`
- No formula, expected-value fixture, runtime, or API behavior changes are claimed.

## P9-01B1 characterization hardening status

- Stage status: `Implemented` (`TestOnly`).
- Added hardening artifacts:
  - `docs/validation/iso52016-matrix-solver-characterization-hardening.md`
  - `docs/validation/iso52016-matrix-solver-characterization-hardening.json`
  - `docs/validation/iso52016-matrix-solver-characterization-hardening.schema.json`
- Added focused hardening tests:
  - `Iso52016MatrixAssemblyInvariantTests`
  - `Iso52016LoadVectorCharacterizationTests`
  - `Iso52016SolverKernelCharacterizationTests`
  - `Iso52016MultiZoneCouplingCharacterizationTests`
  - `Iso52016DiagnosticsResultMappingCharacterizationTests`
- No formula, expected-value fixture, runtime, or API behavior changes are claimed.

## Risks

- Over-aggressive decomposition without characterization expansion can mask subtle numerical drift.
- Current duplication seams increase maintenance effort and naming drift risk.
- Report/diagnostics coupling to calculation internals may reduce clarity of ownership boundaries.

## Next steps

- Use `P9-01B1` hardening baseline as extraction precondition for `P9-01B2..P9-01B6`.
- Keep retained gaps explicit (especially multi-zone absolute pinned coupling anchors) before moving to deeper seam extraction.
