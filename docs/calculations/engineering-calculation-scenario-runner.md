# Engineering Calculation Scenario Runner

## Purpose

Engineering Calculation Scenario Runner is a foundation-level orchestration layer for end-to-end workflow execution.

It coordinates existing validation, topology, load, DHW, system-energy, trace, and reporting services.

It does not implement new calculation physics.

## Scenario Kinds

- `HeatingCoolingOnly`
- `DomesticHotWaterOnly`
- `SystemEnergyOnly`
- `FullEngineeringCore`
- `ValidationOnly`
- `ReportOnly`
- `TraceOnly`

## Execution Modes

- `ValidateOnly`: returns deterministic diagnostics only; no module execution.
- `PrepareOnly`: prepares scenario execution context; no module execution.
- `ExecuteAvailableModules`: executes modules with available structured inputs and skips missing modules with diagnostics.
- `ExecuteFullRequired`: fails validation when required module inputs are missing.
- `DryRun`: returns deterministic execution plan without invoking calculators.

## Pipeline Order

1. Pre-validation and request normalization.
2. Thermal topology normalization (Stage 1 service path).
3. Weather/solar readiness interpretation.
4. Natural ventilation execution when structured inputs are available.
5. Ground boundary execution when structured inputs are available.
6. Heating/cooling execution through existing load-calculation facade when available.
7. DHW execution through existing DHW foundation services when structured demand is available.
8. System-energy execution through existing system-energy foundation services when loads are available.
9. Calculation Trace assembly through Stage 6 trace services.
10. Engineering Report generation/export through Stage 7 report services.

## Diagnostics Behavior

- Diagnostics are aggregated across pre-validation and module execution.
- Diagnostics are deduplicated and sorted deterministically.
- Assumptions, warnings, and errors remain separated.
- Partial execution produces explicit skipped-module diagnostics.

## Trace and Report Integration

- Trace is optional and built from executed and skipped module steps.
- Report is optional and generated from available scenario outputs.
- JSON/Markdown exports are optional and returned in response payload when requested.

## API Integration

Workflow API integrates runner through:

- `POST /api/v1/engineering-workflow/prepare-calculation` (PrepareOnly orchestration path)
- `POST /api/v1/engineering-workflow/run-calculation` (scenario execution path)

## Frontend Behavior

- Frontend calls runner endpoint in API mode.
- Frontend displays execution status (`Prepared`, `PartiallyExecuted`, `CompletedWithWarnings`, `FailedValidation`, `FailedExecution`).
- Frontend does not execute engineering physics in browser.

## Known Limitations

- foundation runner executes only modules with available structured inputs.
- no hidden external weather calls.
- no fake calculation success.
- not a compliance certificate.
- not external validation evidence.
- no full standard compliance claim.
- production persistence/job queue may be future work.
