# Calculation Legacy Inventory

This inventory is limited to architecture hardening and usage mapping.

Boundary statements:
- No EnergyPlus comparison workflow claim.
- No StandardReference equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No full ISO/EN compliance claim.

## Active calculation path

Primary production path for load endpoints:
- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/BuildingLoadCalculationsController.cs`
- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/FloorLoadCalculationsController.cs`
- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/RoomLoadCalculationsController.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Facades/LoadCalculationsFacade.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Abstractions/Pipeline/IEnergyCalculationPipeline.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Pipeline/EnergyCalculationPipelineService.cs`

ISO52016 simulation path (separate API lane):
- `src/Backend/AssistantEngineer.Api/Controllers/Analysis/BuildingEnergyAnalysisController.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Matrix/Iso52016MatrixRoomEnergySimulationService.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Physical/Iso52016PhysicalRoomEnergySimulationService.cs`

## Compatibility / legacy path

Confirmed direct usages from repository scan (`rg` across `src` + `tests`, 2026-05-13):

- No remaining compatibility legacy load services in `src`.
- Load endpoints remain facade/pipeline-driven.

Retired in Phase 5:
- `BuildingEnergyBalanceService` (implementation deleted; DI registration removed; backend source reintroduction guard added).

Retired in Phase 6:
- `FloorCalculationService` (implementation deleted; DI registration removed; backend source reintroduction guard extended).

Retired in Phase 7:
- `BuildingCoolingLoadService` (compatibility test dependency migrated to active facade/pipeline path, DI registration removed, implementation deleted, backend source reintroduction guard extended).

Retired in Phase 8:
- `RoomCalculationService` (compatibility validation test migrated to active facade/pipeline path, DI registration removed, implementation deleted, backend source reintroduction guard extended).

Retired in Phase 9:
- `BuildingHeatingLoadService` (heating/report compatibility test dependencies migrated to active facade/pipeline path, DI registration removed, implementation deleted, backend source reintroduction guard extended).

Important production-path confirmation:
- First-party load controllers call `ILoadCalculationsFacade`.
- `LoadCalculationsFacade` depends on `IEnergyCalculationPipeline`.
- `IEnergyCalculationPipeline` resolves to `EnergyCalculationPipelineService` in composition.
- No direct controller/facade dependency on retired legacy load services was found.
- Separate guard blocks reintroduction of retired legacy load services into backend source.

Preview services (active, do not classify as removable legacy):
- `NaturalVentilationPreviewService` is used by `RoomVentilationController` through `VentilationAnalysisFacade`.
- `GroundTemperatureProfilePreviewService` is used directly by `GroundTemperatureController`.

## Deprecated candidates

- No immediate legacy-load retirement candidates remain in this inventory scope.

## Do not remove yet

Keep for now:
- `EnergyCalculationPipelineService` (active production orchestrator for load endpoints; out of scope for removal).
- `NaturalVentilationPreviewService` and `GroundTemperatureProfilePreviewService` (active endpoints).
- Any service with explicit active endpoint ownership and no approved replacement path.

## Migration notes

Confirmed migration recommendation:
1. Keep load calculation entrypoints on `ILoadCalculationsFacade` and `IEnergyCalculationPipeline`.
2. Keep architecture guards proving controllers and facades stay pipeline-driven.
3. Keep removing compatibility-only services only after test/report coverage is migrated to active path.
4. Keep reintroduction guards updated when a service is retired.

Replacement path:
- Building/Floor/Room/Balance calculations: `ILoadCalculationsFacade` -> `IEnergyCalculationPipeline` -> `EnergyCalculationPipelineService`.
- Annual energy simulation/analysis lane: `IBuildingEnergyAnalysisFacade` + annual-energy adapters where applicable.
- Reporting lane: report calculation services consume `ILoadCalculationsFacade`.

Safe removal conditions (must all be true):
- No registrations in composition roots.
- No references in controllers/facades/reporting services.
- No compatibility tests relying on constructors/behavior.
- Full `dotnet build`, `dotnet test`, and engineering-core release-ready verification pass after removal.

Tests covering active behavior:
- `tests/AssistantEngineer.Tests/Calculations/HeatingLoadValidationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/EnergyCalculationPipelineServiceTests.cs`
- `tests/AssistantEngineer.Tests/Reporting/BuildingHeatingReportDataServiceTests.cs`
- `tests/AssistantEngineer.Tests/Architecture/LegacyCalculationServiceDependencyGuardTests.cs`

## Risk notes

Current risks:
- Hidden third-party/internal direct DI usage cannot be ruled out from first-party repository scan alone.
- `EnergyCalculationPipelineService` remains a large orchestration component; migration should remain incremental.
