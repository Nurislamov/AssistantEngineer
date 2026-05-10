# Engineering Workflow API

## Purpose

Engineering Workflow API is a foundation-level backend orchestration surface for frontend workflow integration.

It aggregates existing building, validation, calculation trace, and reporting foundations without duplicating engineering physics inside controllers.

## Endpoints

Base route:

- `api/v1/engineering-workflow`

Endpoints:

- `GET /api/v1/engineering-workflow/{projectId}/state?buildingId={buildingId}`
- `POST /api/v1/engineering-workflow/validate`
- `POST /api/v1/engineering-workflow/prepare-calculation`
- `POST /api/v1/engineering-workflow/run-calculation`
- `POST /api/v1/engineering-workflow/trace-preview`
- `POST /api/v1/engineering-workflow/report`
- `POST /api/v1/engineering-workflow/report/export/json`
- `POST /api/v1/engineering-workflow/report/export/markdown`

## Request/response model

Workflow DTO contracts are defined in:

- `src/Backend/AssistantEngineer.Api/Contracts/EngineeringWorkflow/EngineeringWorkflowDtos.cs`

Core DTO families:

- workflow state and step/status diagnostics;
- validation request/response;
- calculation preparation request/response;
- calculation scenario runner request/response;
- trace preview request/response;
- report generation request/response;
- report export request/response.

## Validation behavior

Validation endpoint performs deterministic workflow-level checks and diagnostics aggregation.

It does not execute full engineering scenario simulation.

Missing or partial data produces diagnostics rather than crash.

## Scenario runner behavior

`run-calculation` uses scenario runner orchestration and supports deterministic execution modes:

- `ValidateOnly`
- `PrepareOnly`
- `ExecuteAvailableModules`
- `ExecuteFullRequired`
- `DryRun`

Runner response contains execution status, executed/skipped modules, diagnostics, optional trace summary, optional report preview, and optional JSON/Markdown exports.

## Trace preview behavior

Trace preview endpoint uses calculation trace foundation services and returns compact deterministic trace output by requested detail level (`Summary`, `Standard`, `Detailed`).

Trace preview is for explainability and audit trail scaffolding.

## Report generation/export behavior

Report endpoint uses engineering report builder foundation.

Export endpoints use report JSON/Markdown exporters and return serialized output payloads.

Partial reports are supported with diagnostics for missing module data.

## Frontend api/dev mode behavior

Frontend `EngineeringWorkflowClient` uses these endpoints in `api` mode.

`dev` mode remains explicit internal adapter behavior and does not represent production-complete workflow execution.

## Known limitations

- API foundation may prepare or preview calculations without executing full production scenario if runner is not wired.
- API foundation may execute available modules partially and report skipped modules with diagnostics.
- Workflow API is not a compliance certificate.
- Reports summarize current internal engineering calculations only.
- Trace explains internal calculation chain only.
- No external validation evidence.
- No full standard compliance claim.

## Future production path

- wire dedicated production calculation runner endpoint(s);
- expand module-level report/trace endpoint coverage;
- keep controllers thin and orchestration-only;
- preserve deterministic diagnostics and non-claim boundaries in API responses.
