# Calculation Legacy Inventory

This inventory is limited to architecture hardening and usage mapping.

Boundary statements:
- No EnergyPlus parity claim.
- No pyBuildingEnergy parity claim.
- No ASHRAE 140 validation claim.
- No full ISO/EN compliance claim.

## Active calculation path

Primary production path for load endpoints:
- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/BuildingLoadCalculationsController.cs`
- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/FloorLoadCalculationsController.cs`
- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/RoomLoadCalculationsController.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Facades/LoadCalculationsFacade.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Pipeline/EnergyCalculationPipelineService.cs`

ISO52016 simulation path (separate API lane):
- `src/Backend/AssistantEngineer.Api/Controllers/Analysis/BuildingEnergyAnalysisController.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Matrix/Iso52016MatrixRoomEnergySimulationService.cs`
- `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Physical/Iso52016PhysicalRoomEnergySimulationService.cs`

## Compatibility / legacy path

Confirmed direct usages from repository scan (`rg` across `src` + `tests`):

| Service | Confirmed source usage | Confirmed test usage | Classification |
|---|---|---|---|
| `BuildingCoolingLoadService` | DI registration only (`LoadCalculationRegistration`) | `Iso52016ClimateDataValidationTests`, DI lifetime test | compatibility legacy service |
| `FloorCalculationService` | DI registration only (`LoadCalculationRegistration`) | DI lifetime test | compatibility legacy service |
| `RoomCalculationService` | DI registration only (`LoadCalculationRegistration`) | `HeatingLoadValidationTests`, DI lifetime test | compatibility legacy service |
| `BuildingEnergyBalanceService` | DI registration only (`EnergyAnalysisRegistration`) | DI lifetime test | compatibility legacy service |
| `BuildingHeatingLoadService` | DI registration only (`LoadCalculationRegistration`) | `HeatingLoadValidationTests`, `BuildingHeatingReportDataServiceTests` (stub usage), DI lifetime test | compatibility bridge (heating/report test lane) |

Important production-path confirmation:
- First-party load controllers call `ILoadCalculationsFacade`.
- `LoadCalculationsFacade` calls `EnergyCalculationPipelineService`.
- No direct controller/facade dependency on the legacy services above was found.

Preview services (active, do not classify as removable legacy):
- `NaturalVentilationPreviewService` is used by `RoomVentilationController` through `VentilationAnalysisFacade`.
- `GroundTemperatureProfilePreviewService` is used directly by `GroundTemperatureController`.

## Deprecated candidates

Safe deprecation candidates (documentation-level in current pass):
- `BuildingCoolingLoadService`
- `FloorCalculationService`
- `RoomCalculationService`
- `BuildingEnergyBalanceService`

Current deprecation marker strategy:
- Documentation-level markers in service XML remarks.
- `[Obsolete]` is intentionally not used yet because warnings-as-errors policy can break build for existing DI/test references.

Conditional candidate (next wave only after migration checks):
- `BuildingHeatingLoadService`
- Reason: no first-party controller path dependency was found, but report-lane tests and stubs still use it.

## Do not remove yet

Keep for now:
- `EnergyCalculationPipelineService` (active production orchestrator for load endpoints; out of scope for removal).
- `BuildingHeatingLoadService` (still referenced by heating/report compatibility tests).
- `NaturalVentilationPreviewService` and `GroundTemperatureProfilePreviewService` (active endpoints).
- Any service with explicit compatibility tests that still define accepted behavior.

## Migration notes

Confirmed migration recommendation:
1. Freeze legacy services as compatibility-only (done with documentation markers in class remarks).
2. Add/keep architecture tests proving controllers and facade remain pipeline-driven.
3. Move remaining internal consumers (if discovered later) to `ILoadCalculationsFacade` / `EnergyCalculationPipelineService`.
4. Remove DI registrations only after zero runtime consumers and updated tests.
5. Remove implementation files only after DI removal and targeted compatibility test migration.

Replacement path:
- Building/Floor/Room/Balance calculations: `ILoadCalculationsFacade` -> `EnergyCalculationPipelineService`.
- Reporting lane: `BuildingHeatingReportCalculationService` -> `ILoadCalculationsFacade` (already aligned).

Safe removal conditions (must all be true):
- No registrations in composition roots.
- No references in controllers/facades/reporting services.
- No compatibility tests relying on constructors/behavior.
- Full `dotnet build` and `dotnet test` pass after removal.

Tests covering old behavior (keep while migration is incomplete):
- `tests/AssistantEngineer.Tests/Calculations/Iso52016ClimateDataValidationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/HeatingLoadValidationTests.cs`
- `tests/AssistantEngineer.Tests/Calculations/CalculationsDependencyInjectionTests.cs`
- `tests/AssistantEngineer.Tests/Reporting/BuildingHeatingReportDataServiceTests.cs`

## Risk notes

Current risks:
- Hidden third-party/internal direct DI usage cannot be ruled out from first-party repository scan alone.
- Removing compatibility services now can break compatibility tests even if production controllers are unaffected.
- `FloorCalculationService` has weak direct behavioral test coverage (mostly DI-level), so removal confidence is currently low.
- `EnergyCalculationPipelineService` remains a large orchestration component; migration should remain incremental.
