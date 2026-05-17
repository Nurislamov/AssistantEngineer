# Protected Execution Endpoints Rollout

## Purpose

This document records P5-12 controlled rollout of execution-endpoint protection for workflow execution and calculation-run routes without broad report/artifact lock-down.

## Scope

This rollout covers:

- workflow execution endpoints:
  - `POST /api/v{version:apiVersion}/engineering-workflow/prepare-calculation`;
  - `POST /api/v{version:apiVersion}/engineering-workflow/run-calculation`;
  - `POST /api/v{version:apiVersion}/engineering-workflow/jobs`;
  - `POST /api/v{version:apiVersion}/engineering-workflow/jobs/{jobId}/cancel`.
- calculation run endpoints:
  - `GET /api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations/cooling-load`;
  - `GET /api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations/heating-load`;
  - `GET /api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations/energy-balance`;
  - `GET /api/v{version:apiVersion}/floors/{floorId:int}/load-calculations/cooling-load`;
  - `GET /api/v{version:apiVersion}/floors/{floorId:int}/load-calculations/heating-load`;
  - `GET /api/v{version:apiVersion}/rooms/{roomId:int}/load-calculations/cooling-load`;
  - `GET /api/v{version:apiVersion}/rooms/{roomId:int}/load-calculations/heating-load`.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.

## Selected endpoint groups

- `EngineeringWorkflowController` selected execution endpoints (`prepare-calculation`, `run-calculation`, `jobs`, `jobs/{jobId}/cancel`).
- `BuildingLoadCalculationsController`, `FloorLoadCalculationsController`, and `RoomLoadCalculationsController` execution endpoints.

Report generation/export and artifact read/write endpoints are intentionally outside P5-12 scope.
Workflow read/history protection is intentionally handled in a separate P5-14 rollout (`docs/security/protected-workflow-read-history-rollout.md`).

## Workflow execution policy

- Required permission: `WorkflowsExecute`.
- Workflow routes are protected only when:
  - `ApiAuthorization:Enabled=true`;
  - `ApiAuthorization:EnableExecutionEndpointProtectionPilot=true`;
  - `ApiAuthorization:RequireWorkflowExecuteAuthorization=true`.
- Scope resolution order:
  - workflow scope via `IWorkflowAccessScopeResolver` when workflow id is available;
  - building scope fallback when `buildingId` is available;
  - project scope fallback when `projectId` is available;
  - permission-only fallback when no route/resource scope is available.

## Calculation run policy

- Required permission: `WorkflowsExecute` for this rollout stage (no separate `CalculationRun` permission is introduced in P5-12).
- Calculation routes are protected only when:
  - `ApiAuthorization:Enabled=true`;
  - `ApiAuthorization:EnableExecutionEndpointProtectionPilot=true`;
  - `ApiAuthorization:RequireCalculationRunAuthorization=true`.
- Scope resolution order:
  - room scope (`roomId`) via `IRoomAccessScopeResolver` and building fallback;
  - floor scope (`floorId`) via `IFloorAccessScopeResolver` and building fallback;
  - building scope (`buildingId`) via `IBuildingReadAccessScopeResolver`;
  - project scope (`projectId`) via `IProjectReadAccessScopeResolver`.

## Resource scope resolution

- Added resolver abstractions:
  - `IWorkflowAccessScopeResolver`;
  - `IFloorAccessScopeResolver`;
  - `IRoomAccessScopeResolver`.
- `DefaultWorkflowAccessScopeResolver` resolves workflow scope through scenario/job metadata when available and falls back to project/building scope checks when tenant metadata is incomplete.
- `DefaultFloorAccessScopeResolver` and `DefaultRoomAccessScopeResolver` map floor/room resources to building/project scope via existing repositories.
- This step provides route-level gate enforcement, not full query-level tenant filtering.

## Compatibility defaults

Compatibility-safe defaults remain:

- `ApiAuthorization:Enabled=false`;
- `ApiAuthorization:EnableExecutionEndpointProtectionPilot=false`;
- `ApiAuthorization:RequireWorkflowExecuteAuthorization=false`;
- `ApiAuthorization:RequireCalculationRunAuthorization=false`;
- `ApiAuthorization:ReturnNotFoundForTenantMismatch=false`;
- `ApiAuthorization:AllowAnonymousInDevelopment=true`.

Default local/dev behavior remains compatible until execution rollout flags are explicitly enabled.

## Tenant mismatch behavior

- `ApiAuthorization:ReturnNotFoundForTenantMismatch=false` => mismatch returns `403`.
- `ApiAuthorization:ReturnNotFoundForTenantMismatch=true` => mismatch returns `404`.
- Responses avoid tenant scope detail leakage.

## Rate limiting relationship

- Execution endpoint protection in P5-12 aligns with `WorkflowExecute` and `CalculationRun` categories in `docs/security/rate-limiting-policy-registry.json`.
- This rollout does not enable rate limiting globally by default.
- Authorization and rate limiting remain separate controls and are independently feature-flagged.

## Audit/observability behavior

- Structured authorization-denied logs are emitted by the reusable authorization gate with scoped identifiers and without secrets/payload logging.
- Explicit P5-12 audit event emission for execution authorization decisions/start events is deferred to a future stage to avoid fragile coupling.
- Deferred audit integration means audit-writer failures cannot alter endpoint decision flow in this rollout.

## Test matrix

- Execution pilot disabled preserves compatibility behavior for protected execution routes.
- Workflow execution protection enabled + no credentials => `401`.
- Workflow execution protection enabled + missing `WorkflowsExecute` => `403`.
- Workflow execution protection enabled + matching permission/scope => success.
- Calculation run protection enabled + no credentials => `401`.
- Calculation run protection enabled + missing `WorkflowsExecute` => `403`.
- Calculation run protection enabled + matching permission/scope => non-auth path proceeds.
- Tenant mismatch behavior is validated as `403/404` based on `ReturnNotFoundForTenantMismatch`.
- Report/artifact endpoints remain outside P5-12 protection scope unless explicitly selected in future stages.

## What remains unprotected

- Report generation authorization rollout.
- Artifact write/delete authorization rollout.
- Full workflow-id ownership resolver implementation.
- Complete tenant-boundary enforcement at persistence/query boundaries.
- Distributed rate limiting and durable audit event persistence integration.

## Next rollout candidates

- P5-13 controlled report/artifact rollout (`docs/security/protected-report-artifact-endpoints-rollout.md`).
- Report generate/export authorization (`ReportsWrite`) with read/write separation.
- Artifact management authorization (`ArtifactRead`/`ArtifactWrite`) with ownership checks.
- Workflow-history and execution-read endpoints (`WorkflowsRead`) with scoped ownership enforcement.
- Deeper query-level tenant filtering and durable authorization/audit observability integration.

P5-15 tenant isolation note:

- Cross-tenant expectations for `WorkflowsExecute` and calculation execution are tracked in `docs/security/tenant-isolation-integration-matrix.md`.
- P5-15 adds matrix coverage for same-tenant execution, missing execute permission, anonymous requests, and tenant mismatch anti-enumeration behavior.
