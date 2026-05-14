# EnergyCalculationPipeline Use-Case Split Plan

## Scope
- Date: 2026-05-14.
- Goal of this step: characterization safety-net + decomposition plan.
- Non-goals:
- No calculation physics changes.
- No public API route changes.
- No DTO contract changes.
- No expected numeric value changes.
- No removal of legacy services.
- No pipeline semantic changes.
- No new engineering assumptions.

## Progress update (equipment sizing extraction)
- Date: 2026-05-14.
- Status: first low-risk use-case extraction completed.
- Extracted:
- `IEquipmentSizingCalculationUseCase`
- `EquipmentSizingCalculationUseCase`
- `EnergyCalculationPipelineService.CalculateRoomEquipmentSizingAsync` now delegates to the extracted use case.
- Behavior policy:
- Validation messages/order preserved (`system type`, `unit type`, `provider not configured`, `room not found`).
- Catalog sizing behavior preserved (same orchestrator, same candidate mapping, same diagnostics policy gate).
- Numerical behavior preserved by keeping the same room-load input assembly and equipment sizing engine path.

## Progress update (system energy handoff extraction)
- Date: 2026-05-14.
- Status: system-energy handoff orchestration extracted from main pipeline class.
- Extracted:
- `ISystemEnergyHandoffUseCase`
- `SystemEnergyHandoffUseCase`
- Main pipeline now delegates `CalculateBuildingSystemEnergyFromUsefulDemandAsync` to use-case.
- Useful-demand source:
- Use-case consumes a useful-demand provider that resolves building annual useful demand via pipeline energy-balance path, preserving current useful-demand behavior.
- Behavior policy:
- EN15316-inspired path semantics preserved.
- Opt-in options gate semantics preserved.
- Validation messages/order preserved for `services not configured` and `options disabled` guards.

## Current public surface inventory

### `IEnergyCalculationPipeline` methods
- `CalculateRoomCoolingLoadAsync`
- `CalculateRoomHeatingLoadAsync`
- `CalculateFloorCoolingLoadAsync`
- `CalculateFloorHeatingLoadAsync`
- `CalculateBuildingCoolingLoadAsync`
- `CalculateBuildingHeatingLoadAsync`
- `CalculateBuildingEnergyBalanceAsync`
- `CalculateRoomEquipmentSizingAsync`

### Additional public `EnergyCalculationPipelineService` methods
- `CalculateRoomLoadAsync` (internal room-load application result used by multiple paths)
- `CalculateFloorLoadAsync` (shared floor aggregation entrypoint)
- `CalculateBuildingSystemEnergyFromUsefulDemandAsync` (system-energy handoff orchestrator)

## Responsibility groups (current orchestrator)

### 1) Room load use case
- Load room and preferences.
- Resolve climate context and room context (ground/solar/internal gains).
- Execute `RoomLoadCalculationEngine`.
- Map to room cooling/heating/application DTOs with diagnostics policy.

### 2) Floor aggregation use case
- Load floor and preferences.
- Resolve climate context.
- Aggregate room loads via `LoadAggregationEngine`.
- Map to floor result and preserve method compatibility diagnostics.

### 3) Building aggregation use case
- Load building and preferences.
- Resolve climate context.
- Aggregate room/floor/building loads.
- Compose building heating result room details.

### 4) Annual energy balance use case
- Load building and validate climate prerequisites.
- Delegate source calculation to legacy energy calculator.
- Adapt source through annual adapter and `AnnualEnergyBalanceEngine`.
- Map diagnostics and annual/method metadata.

### 5) Equipment sizing use case
- Validate sizing request input.
- Load room and compute room load dependency.
- Validate room diagnostics and execute equipment sizing orchestrator.
- Return recommended/rejected candidates with diagnostics.

### 6) System energy handoff use case
- Validate configuration/opt-in gates.
- Build useful-demand source from annual energy balance.
- Build EN15316-inspired handoff.
- Execute system-energy engine and compose final result.

## Target decomposition (no behavior changes)

### Proposed use-case interfaces
- `IRoomLoadUseCase`
- `IFloorLoadAggregationUseCase`
- `IBuildingLoadAggregationUseCase`
- `IAnnualEnergyBalanceUseCase`
- `IRoomEquipmentSizingUseCase`
- `ISystemEnergyHandoffUseCase`

### Proposed orchestrator shell
- Keep `EnergyCalculationPipelineService` as thin facade implementing `IEnergyCalculationPipeline`.
- Facade delegates each public method to a focused use-case service.
- Preserve current default method values, error messages, and mapping conventions.

## Migration order (small safe slices)
1. Extract `IRoomLoadUseCase` around `CalculateRoomLoadAsync` dependencies.
2. Extract floor aggregation use case (delegating from `CalculateFloorLoadAsync`).
3. Extract building aggregation use case (cooling/heating paths).
4. Extract annual energy balance use case.
5. Extract equipment sizing use case (depends on room load).
6. Extract system energy handoff use case (depends on annual energy balance).
7. Keep `EnergyCalculationPipelineService` as compatibility facade until downstream consumers are stable.

## Dependency reduction plan
- Phase A: constructor slimming by grouping collaborators per use case:
- Room load cluster: repositories + room engine + climate/room context helpers.
- Aggregation cluster: aggregation engine + result assemblers/diagnostics policy.
- Annual/system cluster: legacy energy calculator + annual adapter/engine + system handoff services/options.
- Sizing cluster: equipment sizing engine/provider + room load dependency.
- Phase B: move clusters behind use-case interfaces and keep composition root wiring stable.
- Phase C: preserve existing partial helper files as implementation details under extracted use cases.

## Risk matrix
- Risk: subtle numeric drift after moving orchestration boundaries.
- Impact: high.
- Mitigation: characterize per public method before extraction; compare deterministic fixture outputs.
- Risk: diagnostics text/code drift.
- Impact: medium-high.
- Mitigation: assert key diagnostic codes/messages in characterization tests.
- Risk: not-found/validation semantics drift.
- Impact: high.
- Mitigation: explicit tests for `NotFound`/`Validation` branches by method group.
- Risk: constructor/DI regression while splitting services.
- Impact: medium.
- Mitigation: keep facade contract unchanged; enforce DI registration tests.

## Required test gates before each extraction slice
- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- Keep and extend `EnergyCalculationPipelineServiceTests` characterization matrix:
- Room: not found, validation, happy path, diagnostics.
- Floor: not found, validation, happy path, diagnostics.
- Building aggregation: not found, validation, happy path, diagnostics.
- Annual energy balance: not found, validation, happy path, diagnostics.
- Equipment sizing: not found, validation, happy path, diagnostics.
- System handoff: not found, validation, happy path, diagnostics.
- Keep existing architecture guard tests green (`P3EnergyCalculationPipelineRefactorGuardTests`, module dependency guards).

## Compatibility policy during split
- Preserve `IEnergyCalculationPipeline` public surface.
- Preserve `EnergyCalculationPipelineService` method behavior and return semantics.
- Preserve public DTO shapes and field names.
- Preserve current error text where tests/assertions already depend on it.
- No claims of additional guarantees beyond current behavior.
