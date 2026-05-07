# Calculation Legacy Retirement Plan

Scope: retirement preparation only. No runtime removals are performed in this phase.

Boundary statements:
- No EnergyPlus parity claim.
- No pyBuildingEnergy parity claim.
- No ASHRAE 140 validation claim.
- No full ISO/EN compliance claim.

## Baseline policy

- Legacy services remain available for compatibility while migration closes.
- Direct first-party controller/facade dependencies on legacy services are prohibited by architecture guards.
- Allowed production references are fenced to compatibility definitions and DI composition roots.
- DI registrations stay in place until all removal gates are satisfied.

## BuildingCoolingLoadService

### Current allowed usages

- Service definition: `Application/Services/Buildings/BuildingCoolingLoadService.cs`
- DI registration: `Composition/LoadCalculationRegistration.cs`

### Tests depending on it

- `tests/AssistantEngineer.Tests/Calculations/Iso52016ClimateDataValidationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

### Replacement path

- `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService` for first-party cooling calculations.

### Removal gates

1. No references outside service definition and DI composition.
2. Compatibility tests migrated to facade/pipeline behavior checks.
3. DI registration removed and solution build/tests remain green.

### Risk level

- Medium: compatibility tests still touch behavior/registration.

### Future PR sequence

1. Migrate remaining tests to facade-first assertions.
2. Remove DI registration.
3. Remove service implementation and update inventory/report docs.

## FloorCalculationService

### Current allowed usages

- Service definition: `Application/Services/Floors/FloorCalculationService.cs`
- DI registration: `Composition/LoadCalculationRegistration.cs`

### Tests depending on it

- `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

### Replacement path

- `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService` for floor aggregation.

### Removal gates

1. Confirm no runtime/facade/controller dependencies.
2. Add direct facade coverage for floor path where missing.
3. Remove DI registration and verify full solution gate.

### Risk level

- Medium: thin direct tests can hide late coupling.

### Future PR sequence

1. Add/confirm facade-level floor regression tests.
2. Remove DI registration.
3. Remove implementation and refresh architecture inventory.

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

- Service definition: `Application/Services/Buildings/BuildingEnergyBalanceService.cs`
- DI registration: `Composition/EnergyAnalysisRegistration.cs`

### Tests depending on it

- `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`

### Replacement path

- `IBuildingEnergyAnalysisFacade` and annual energy pipeline adapters.

### Removal gates

1. Confirm no API/facade direct dependency on the legacy service type.
2. Verify evidence/traceability tests continue to pass via current facades.
3. Remove DI registration and run full verification gate.

### Risk level

- Low to medium: mostly registration-level coupling, but evidence paths must stay stable.

### Future PR sequence

1. Add explicit facade-level energy-balance guard assertions if needed.
2. Remove registration.
3. Remove implementation and refresh legacy inventory.

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
