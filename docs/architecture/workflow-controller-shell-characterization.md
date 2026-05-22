# Workflow Controller Shell Characterization (P8-03D)

## Purpose

Freeze current `EngineeringWorkflowController` shell behavior before P8-03E/P8-03F decomposition work.

## Scope

- `EngineeringWorkflowController` partials:
  - `EngineeringWorkflowController.cs`
  - `EngineeringWorkflowController.ReadHistory.cs`
  - `EngineeringWorkflowController.ReportArtifact.cs`
- Route/action signatures for execution, read/history, and report/artifact endpoint groups.
- Status-code and response-shape behavior characterization at API shell level.
- Authorization-gate interaction compatibility for workflow/read/report policies.

## Non-claims

- No workflow behavior change claim.
- No public API route change claim.
- No DTO shape change claim.
- No calculation physics change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Current controller partials

- `EngineeringWorkflowController.cs`: validate, prepare/run, jobs create/cancel, authorization adapter helpers.
- `EngineeringWorkflowController.ReadHistory.cs`: state/scenario/job read and list actions, tenant-scoped read branching.
- `EngineeringWorkflowController.ReportArtifact.cs`: trace/report/export and scenario artifact read actions.

## Characterized endpoint groups

- Execution:
  - `POST /api/v1/engineering-workflow/prepare-calculation`
  - `POST /api/v1/engineering-workflow/run-calculation`
  - `POST /api/v1/engineering-workflow/jobs`
  - `POST /api/v1/engineering-workflow/jobs/{jobId}/cancel`
- Read/history:
  - `GET /api/v1/engineering-workflow/{projectId}/state`
  - `GET /api/v1/engineering-workflow/scenarios/{scenarioId}`
  - `GET /api/v1/engineering-workflow/{projectId}/scenarios`
  - `GET /api/v1/engineering-workflow/jobs/{jobId}`
  - `GET /api/v1/engineering-workflow/jobs/{jobId}/events`
  - `GET /api/v1/engineering-workflow/{projectId}/jobs`
- Report/artifact:
  - `POST /api/v1/engineering-workflow/trace-preview`
  - `POST /api/v1/engineering-workflow/report`
  - `POST /api/v1/engineering-workflow/report/export/json`
  - `POST /api/v1/engineering-workflow/report/export/markdown`
  - `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts`
  - `GET /api/v1/engineering-workflow/scenarios/{scenarioId}/artifacts/{artifactKind}`

## Route/action signature compatibility

- Route templates and action names are locked by reflection-based signature tests.
- DTO binding points are locked for key request/response actions.
- Return-type category (`Task<ActionResult<...>>`) and route-parameter positions are characterized.

## Status code compatibility

- Characterized behavior includes:
  - success (`200`) for default execution/read/report flows under compatibility defaults;
  - `404` for missing scenario/job and missing valid artifact;
  - `400` for invalid artifact kind;
  - `401/403/404` policy outcomes on protected workflow/report routes based on auth/options matrix.

## Response shape compatibility

- Key DTO contract shape is characterized without full brittle snapshots:
  - workflow state core fields;
  - report export payload fields;
  - paged read/list envelope fields.

## Authorization interaction compatibility

- Execution routes continue to require `WorkflowsExecute` when execution protection is enabled.
- Workflow read routes continue to require `WorkflowsRead` when workflow-read protection is enabled.
- Report/artifact routes continue to use report/artifact policy requirements and are not downgraded by workflow-read permission.
- Tenant mismatch anti-enumeration behavior remains option-driven (`403` vs `404`) for workflow-read scope checks.

## Persistence/job behavior compatibility

- Prepare/run/jobs/cancel flows are characterized as API shell behavior with current in-memory/default integration setup.
- Job cancel unknown-id behavior remains `404` with `CALCULATION_JOB_NOT_FOUND`.
- Read-history pagination envelopes remain stable for project jobs/scenarios list endpoints.

## Known limitations

- `GET /scenarios/{scenarioId}/artifacts` currently returns `200` with empty list when scenario id is unresolved in default compatibility path.
- Workflow-id unresolved fallback behavior remains characterized and unchanged from prior gate characterization stages.
- Characterization is intentionally status/shape focused; deeper orchestration decomposition remains deferred to P8-03E/P8-03F.

## Refactor safety contract

- Do not change route templates or controller action signatures during P8-03E/P8-03F.
- Preserve status-code outcomes and key response-shape fields characterized in this stage.
- Preserve authorization gate interaction semantics and option-driven anti-enumeration behavior.
- Preserve DTO contract names and JSON property surface for characterized payloads.

## Next steps

- P8-03E: safe migration of workflow orchestration helpers from API namespace to module namespace.
- P8-03F: controller partial shell size reduction with unchanged public API behavior.
