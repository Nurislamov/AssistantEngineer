# P1 Scenario Runner Decomposition Status

## Scope

P1 decomposes `EngineeringCalculationScenarioRunner` without changing calculation physics or public scenario DTOs.

## Completed

### P1-01 вЂ” Module execution extraction

- Added `IEngineeringCalculationScenarioModuleExecutor`.
- Moved module timing, executed/skipped/failed outcome mapping, and module result materialisation out of the runner.
- Added architecture and unit tests guarding the extraction.

### P1-02 вЂ” Scenario result/finalization builder extraction

- Added `IEngineeringCalculationScenarioResultBuilder`.
- Moved trace construction, report export, module summary lookup, and final status determination out of the runner.
- Removed direct trace/report dependencies from the runner.

### P1-03 вЂ” Scenario request validation extraction

- Added `IEngineeringCalculationScenarioRequestValidator`.
- Moved scenario preflight validation, diagnostics sorting/deduplication, severity ranking, and blocking error detection out of the runner.
- Added unit tests and architecture guards for the validator boundary.

### P1-04 вЂ” Weather/solar and ventilation step extraction

- Added `IEngineeringCalculationWeatherSolarScenarioStep`.
- Added `IEngineeringCalculationVentilationScenarioStep`.
- Moved weather/solar readiness and natural ventilation readiness/skip rules out of the runner.
- Kept module timing and outcome aggregation in `IEngineeringCalculationScenarioModuleExecutor`.

### P1-05 вЂ” Ground and DHW step extraction

- Added `IEngineeringCalculationGroundScenarioStep`.
- Added `IEngineeringCalculationDomesticHotWaterScenarioStep`.
- Moved ground readiness/skip rules out of the runner.
- Moved DHW metadata handoff, deterministic 8760 useful profile expansion, default loss definition, calculator invocation, and diagnostic adaptation out of the runner.
- Removed direct `IDomesticHotWaterSystemLoadCalculator` dependency from the runner.

### P1-06 вЂ” System energy step extraction

- Added `IEngineeringCalculationSystemEnergyScenarioStep`.
- Moved system-energy metadata load handoff, DHW foundation handoff, default stage/generator/factor definitions, calculator invocation, and diagnostic adaptation out of the runner.
- Removed direct `ISystemEnergyFoundationCalculator` dependency from the runner.

## Remaining P1 work

- Consider extracting thermal-topology and heating/cooling steps if the runner still exceeds the desired orchestration-only size.
- Make queued job execution honest by either disabling queued mode or adding a real background worker.
- Introduce a batch workflow input snapshot boundary before optimizing repository calls.
## P1-07 вЂ” Queued job worker foundation

Implemented a real single-node background worker path for queued calculation jobs.

- `EngineeringCalculationJobWorker` polls queued jobs through `ListQueuedAsync`.
- `EngineeringCalculationJobService.ExecuteQueuedJobAsync` executes existing queued records.
- The old `CALCULATION_JOB_WORKER_NOT_ENABLED` diagnostic was removed.
- Testing keeps the hosted loop disabled for deterministic API tests.

This closes the misleading queued-mode behaviour identified in the technical audit.
## P1-08 workflow input snapshot boundary

Closed. Workflow state input collection now sits behind IEngineeringWorkflowInputSnapshotBuilder; per-room input calls are isolated there until repository-level batching is introduced.
