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
- `POST /api/v1/engineering-workflow/jobs`
- `GET /api/v1/engineering-workflow/jobs/{jobId}`
- `GET /api/v1/engineering-workflow/jobs/{jobId}/events`
- `POST /api/v1/engineering-workflow/jobs/{jobId}/cancel`
- `GET /api/v1/engineering-workflow/{projectId}/jobs`
- `GET /api/v1/engineering-workflow/{projectId}/scenarios`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts`
- `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}`
- `POST /api/v1/engineering-workflow/trace-preview`
- `POST /api/v1/engineering-workflow/report`
- `POST /api/v1/engineering-workflow/report/export/json`
- `POST /api/v1/engineering-workflow/report/export/markdown`

List endpoints use paged response envelopes:

- query: `page`, `pageSize`
- default: `page=1`, `pageSize=50`
- max `pageSize=200` for workflow jobs/scenarios endpoints
- response: `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`

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
- calculation job request/result/event/progress lifecycle payloads.

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

`run-calculation` and `prepare-calculation` also persist scenario records, compact result summaries, diagnostics snapshots, and artifacts in persistence foundation storage.

### Idempotency key (foundation baseline)

Heavy submission endpoints support `Idempotency-Key` header:

- `POST /api/v1/engineering-workflow/run-calculation`
- `POST /api/v1/engineering-workflow/jobs`

Behavior:

- no header: existing behavior remains unchanged;
- first request with key: request is processed and response reference is persisted in idempotency store;
- repeated key with same scope and same request fingerprint: API replays persisted scenario/job reference without rerunning heavy orchestration;
- repeated key with same scope but different request fingerprint: API returns `409 Conflict`.

Scope baseline:

- `projectId + action route + idempotency key`.

Foundation limitation:

- idempotency store is in-memory and local-node only (not distributed, not durable across restarts).

## Job lifecycle behavior

`jobs` endpoints provide persisted lifecycle (`Created`, `Queued`, `Running`, terminal statuses, cancellation request/cancelled states) over the existing scenario runner.

Synchronous mode executes scenario runner in-process and persists job progress/events.

Queued mode remains honest foundation behavior when worker is not enabled and does not fake asynchronous completion.

## Trace preview behavior

Trace preview endpoint uses calculation trace foundation services and returns compact deterministic trace output by requested detail level (`Summary`, `Standard`, `Detailed`).

Trace preview is for explainability and audit trail scaffolding.

## Report generation/export behavior

Report endpoint uses engineering report builder foundation.

Export endpoints use report JSON/Markdown exporters and return serialized output payloads.

Partial reports are supported with diagnostics for missing module data.

## Persistence behavior

Workflow API persists:

- latest workflow state snapshot per project;
- scenario request/response summary records;
- scenario artifacts (`TraceJson`, `ReportJson`, `ReportMarkdown`, `ValidationDiagnostics`, `ScenarioResultJson`);
- scenario history events (`Created`, `Prepared`, `Started`, `Completed`, `Failed`, `ReportGenerated`).
- job records and job events for lifecycle/progress retrieval.

Persistence response payloads remain deterministic and diagnostics-focused.

Persistence snapshots are protected by configurable payload size gates with deterministic truncation marker behavior for oversized JSON/artifact content.

Provider model is documented in:

- `docs/api/engineering-workflow-durable-persistence.md`

Workflow state and scenario responses include persistence metadata:

- `persistence`
- `persistenceProvider`
- `durablePersistenceEnabled`

## Frontend api/dev mode behavior

Frontend `EngineeringWorkflowClient` uses these endpoints in `api` mode.

`dev` mode remains explicit internal adapter behavior and does not represent production-complete workflow execution.

## Known limitations

- API foundation may prepare or preview calculations without executing full production scenario if runner is not wired.
- API foundation may execute available modules partially and report skipped modules with diagnostics.
- API persistence foundation may be in-memory depending on current deployment wiring.
- API persistence durable mode can run on SQLite foundation provider and should not be interpreted as production-certified durability.
- Idempotency baseline is in-memory and local-node only; distributed or durable idempotency store is future work.
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
