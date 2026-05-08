# Calculation Legacy Retirement Plan

Scope: service-by-service legacy retirement with proof-first gates.

Boundary statements:
- No EnergyPlus comparison workflow claim.
- No StandardReference equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No full ISO/EN compliance claim.

## Baseline policy

- Legacy services remain available for compatibility while migration closes, except services that have completed full retirement gates.
- Direct first-party controller/facade dependencies on legacy services are prohibited by architecture guards.
- Allowed production references are fenced to compatibility definitions and DI composition roots for active compatibility services.
- Retired services are blocked from backend source reintroduction by architecture guard tests.
- DI registrations stay in place until all removal gates are satisfied per service.

## Phase 4 pilot analysis

Phase 4 re-scan result: no legacy service passed full removal gates without risky compatibility churn.

Blockers identified from source/test scan:
- `BuildingCoolingLoadService`: direct test instantiation in `Iso52016ClimateDataValidationTests` + DI lifetime guard.
- `FloorCalculationService`: still DI-registered and covered by architecture allowlist/compatibility policy.
- `RoomCalculationService`: direct test instantiation in `HeatingLoadValidationTests` + DI lifetime guard.
- `BuildingEnergyBalanceService`: still DI-registered and explicitly guarded as compatibility service.
- `BuildingHeatingLoadService`: direct test/report-lane dependencies in `HeatingLoadValidationTests` and `BuildingHeatingReportDataServiceTests`.

Decision in Phase 4:
- no runtime removal,
- no DI unregistration,
- no implementation file deletion.

## Phase 5 pilot execution

Selected candidate by priority and risk gates: `BuildingEnergyBalanceService`.

Proof summary before removal:
- No direct controller usage.
- No direct facade usage.
- No active runtime usage outside DI compatibility registration.
- Pipeline-level replacement coverage already present in `EnergyCalculationPipelineServiceTests` for `CalculateBuildingEnergyBalanceAsync` behavior and diagnostics.

Retirement action completed:
- Removed `BuildingEnergyBalanceService` DI registration from `Composition/EnergyAnalysisRegistration.cs`.
- Deleted `Application/Services/Buildings/BuildingEnergyBalanceService.cs`.
- Updated legacy architecture guard to:
  - keep fencing for remaining active compatibility services,
  - block reintroduction of retired `BuildingEnergyBalanceService` in backend source.

Services still blocked for retirement:
- `BuildingCoolingLoadService`: direct compatibility behavior test dependency (`Iso52016ClimateDataValidationTests`) + DI compatibility registration.
- `RoomCalculationService`: direct compatibility behavior test dependency (`HeatingLoadValidationTests`) + DI compatibility registration.
- `BuildingHeatingLoadService`: direct compatibility/report-lane dependencies (`HeatingLoadValidationTests`, `BuildingHeatingReportDataServiceTests`) and high report-lane risk.

## Phase 6 pilot execution

Selected candidate by priority and risk gates: `FloorCalculationService`.

Proof summary before removal:
- No direct controller usage.
- No direct facade usage.
- No runtime usage in application services outside DI compatibility registration.
- Active replacement path coverage confirmed through `EnergyCalculationPipelineService` floor methods:
  - floor cooling/heating aggregation equivalence and method diagnostics,
  - floor not-found behavior,
  - no legacy service constructor dependency.

Retirement action completed:
- Removed `FloorCalculationService` DI registration from `Composition/LoadCalculationRegistration.cs`.
- Deleted `Application/Services/Floors/FloorCalculationService.cs`.
- Updated legacy architecture guard to:
  - keep fencing for remaining active compatibility services,
  - block reintroduction of retired `BuildingEnergyBalanceService` and `FloorCalculationService` in backend source.

Services still blocked for retirement:
- `BuildingCoolingLoadService`: direct compatibility behavior test dependency (`Iso52016ClimateDataValidationTests`) + DI compatibility registration.
- `RoomCalculationService`: direct compatibility behavior test dependency (`HeatingLoadValidationTests`) + DI compatibility registration.
- `BuildingHeatingLoadService`: direct compatibility/report-lane dependencies (`HeatingLoadValidationTests`, `BuildingHeatingReportDataServiceTests`) and high report-lane risk.

## Phase 7 pilot execution

Selected candidate by priority and risk gates: `BuildingCoolingLoadService`.

Proof summary before removal:
- No direct controller usage.
- No direct facade usage.
- No runtime usage in application services outside DI compatibility registration.
- Compatibility behavior coverage migrated to active path:
  - removed direct constructor dependency from `Iso52016ClimateDataValidationTests`,
  - added active-path facade coverage in `EnergyCalculationPipelineServiceTests` for building-cooling validation behavior when critical climate inputs are missing.
- Post-migration scan confirmed no remaining direct test dependency on `BuildingCoolingLoadService`.

Retirement action completed:
- Removed `BuildingCoolingLoadService` DI registration from `Composition/LoadCalculationRegistration.cs`.
- Deleted `Application/Services/Buildings/BuildingCoolingLoadService.cs`.
- Updated legacy architecture guard to:
  - keep fencing for remaining active compatibility services,
  - block reintroduction of retired `BuildingEnergyBalanceService`, `FloorCalculationService`, and `BuildingCoolingLoadService` in backend source.

Services still blocked for retirement:
- `RoomCalculationService`: direct compatibility behavior test dependency (`HeatingLoadValidationTests`) + DI compatibility registration.
- `BuildingHeatingLoadService`: direct compatibility/report-lane dependencies (`HeatingLoadValidationTests`, `BuildingHeatingReportDataServiceTests`) and high report-lane risk.

## BuildingCoolingLoadService

### Current allowed usages

- Retired in Phase 7.
- Runtime source usage: none in `src`.

### Tests depending on it

- None (direct constructor/DI lifetime dependency removed).
- Reintroduction guard: `tests/AssistantEngineer.Tests/Architecture/LegacyCalculationServiceDependencyGuardTests.cs`.

### Replacement path

- `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService` for first-party cooling calculations.

### Removal gates

1. No references outside service definition and DI composition. Completed.
2. Compatibility tests migrated to facade/pipeline behavior checks. Completed.
3. DI registration removed and solution build/tests remain green. Completed.

### Risk level

- Closed in this phase (medium-risk candidate retired after behavior migration proof).

### Future PR sequence

1. Keep reintroduction guard active and evolve only with explicit architectural decision.
2. Continue retirement queue with next candidate (`RoomCalculationService`) after equivalent compatibility test migration.

## FloorCalculationService

### Current allowed usages

- Retired in Phase 6.
- Runtime source usage: none in `src`.

### Tests depending on it

- None (direct constructor/DI lifetime dependency removed).
- Reintroduction guard: `tests/AssistantEngineer.Tests/Architecture/LegacyCalculationServiceDependencyGuardTests.cs`.

### Replacement path

- `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService` for floor aggregation.

### Removal gates

1. Confirm no runtime/facade/controller dependencies. Completed.
2. Add/confirm active floor-path regression coverage outside legacy service. Completed.
3. Remove DI registration and implementation, then run full verification gate. Completed.

### Risk level

- Closed in this phase (low-risk candidate retired after replacement coverage proof).

### Future PR sequence

1. Keep reintroduction guard active and evolve only with explicit architectural decision.
2. Continue retirement queue with next candidate (`RoomCalculationService`) after compatibility behavior tests are migrated to active path.

## RoomCalculationService

### Current allowed usages

- Service definition: `Application/Services/Rooms/RoomCalculationService.cs`
- DI registration: `Composition/LoadCalculationRegistration.cs`

### Tests depending on it

- `tests/AssistantEngineer.Tests/Calculations/HeatingLoadValidationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

### Replacement path

- `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService` for room calculations.

### Removal gates

1. Compatibility tests migrated to pipeline/facade equivalents.
2. No constructor injection usage outside compatibility definition/DI.
3. DI registration removed with green restore/build/test and engineering scripts.

### Risk level

- Medium: validation behavior expectations are sensitive.

### Future PR sequence

1. Port validation tests to pipeline/facade entrypoints.
2. Remove registration.
3. Remove implementation and update docs/tests.

## BuildingEnergyBalanceService

### Current allowed usages

- Retired in Phase 5.
- Runtime source usage: none in `src`.

### Tests depending on it

- None (direct DI lifetime dependency removed).
- Reintroduction guard: `tests/AssistantEngineer.Tests/Architecture/LegacyCalculationServiceDependencyGuardTests.cs`.

### Replacement path

- `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService.CalculateBuildingEnergyBalanceAsync` for load endpoint compatibility path.
- `IBuildingEnergyAnalysisFacade` + annual energy adapters for analysis/simulation lane where applicable.

### Removal gates

1. Confirm no API/facade direct dependency on the legacy service type. Completed.
2. Verify replacement coverage through active facade/pipeline path. Completed.
3. Remove DI registration and implementation, then run full verification gate. Completed.

### Risk level

- Closed in this phase (low-risk candidate retired).

### Future PR sequence

1. Keep reintroduction guard active and evolve only with explicit architectural decision.
2. Continue retirement queue with next candidate (`RoomCalculationService`) after compatibility behavior tests are migrated to active path.

## BuildingHeatingLoadService

### Current allowed usages

- Service definition: `Application/Services/Buildings/BuildingHeatingLoadService.cs`
- DI registration: `Composition/LoadCalculationRegistration.cs`
- Compatibility/report lane stubs in tests.

### Tests depending on it

- `tests/AssistantEngineer.Tests/Calculations/HeatingLoadValidationTests.cs`
- `tests/AssistantEngineer.Tests/Reporting/BuildingHeatingReportDataServiceTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

### Replacement path

- `ILoadCalculationsFacade` for primary heating results.
- Reporting continues to consume facade results and report contracts.

### Removal gates

1. Report-lane tests and stubs migrated off legacy service shape.
2. No compatibility constructor dependency remains.
3. DI registration removed and full engineering-core verify/release-ready scripts pass.

### Risk level

- High: still tied to report-lane compatibility expectations.

### Future PR sequence

1. Migrate report-lane tests to facade contracts.
2. Remove heating legacy registration.
3. Remove implementation only after full release-ready verification.
