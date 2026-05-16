# Protected Write Endpoints Rollout

## Purpose

This document records P5-11 controlled rollout of write-endpoint protection for Projects/Buildings without broad workflow/calculation/report lock-down.

## Scope

This rollout covers:

- `POST /api/v{version:apiVersion}/projects`;
- `PUT /api/v{version:apiVersion}/projects/{id:int}`;
- `DELETE /api/v{version:apiVersion}/projects/{id:int}`;
- `POST /api/v{version:apiVersion}/projects/{projectId:int}/buildings`;
- `POST /api/v{version:apiVersion}/projects/{projectId:int}/buildings/from-archetype`;
- `PUT /api/v{version:apiVersion}/buildings/{id:int}`;
- `DELETE /api/v{version:apiVersion}/buildings/{id:int}`.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.

## Selected endpoint groups

- `ProjectsController` write endpoints (create/update/delete).
- `BuildingsController` write endpoints (create/from-archetype/update/delete).

Workflow execute, calculation run, report generation, and artifact operations are intentionally out of scope in P5-11.

## Protection behavior

Write protection is enforced only when:

- `ApiAuthorization:Enabled=true`;
- `ApiAuthorization:EnableWriteEndpointProtectionPilot=true`;
- endpoint-group switch is enabled:
  - `ApiAuthorization:RequireProjectWriteAuthorization=true` for project writes;
  - `ApiAuthorization:RequireBuildingWriteAuthorization=true` for building writes.

Failure behavior:

- pilot disabled => existing behavior;
- unauthenticated => `401`;
- authenticated without required permission => `403`;
- tenant mismatch => `403` or `404` according to `ReturnNotFoundForTenantMismatch`.

## Compatibility defaults

Compatibility-safe defaults remain:

- `ApiAuthorization:Enabled=false`;
- `ApiAuthorization:EnableWriteEndpointProtectionPilot=false`;
- `ApiAuthorization:RequireProjectWriteAuthorization=false`;
- `ApiAuthorization:RequireBuildingWriteAuthorization=false`;
- `ApiAuthorization:ReturnNotFoundForTenantMismatch=false`;
- `ApiAuthorization:AllowAnonymousInDevelopment=true`.

Defaults keep local/dev behavior compatible unless rollout flags are explicitly enabled.

## Project write policy

- Create project: requires authenticated principal with `ProjectsWrite` when write pilot is enabled.
- Update/delete project: require `ProjectsWrite` plus project-scope authorization check based on route `projectId`.
- No claim is made that project write tenant isolation is fully complete beyond staged gate checks.

## Building write policy

- Create/from-archetype building: requires `BuildingsWrite` against parent project scope.
- Update/delete building: requires `BuildingsWrite` plus building-scope authorization check from route `buildingId`.
- Policy does not expand workflow or calculation authorization in this stage.

## Tenant mismatch behavior

- `ApiAuthorization:ReturnNotFoundForTenantMismatch=false` => mismatch returns `403`.
- `ApiAuthorization:ReturnNotFoundForTenantMismatch=true` => mismatch returns `404`.
- Responses avoid tenant mismatch detail leakage.

## Audit/observability behavior

- Observability: structured authorization-denied logs are emitted from the reusable authorization gate for project/building scope checks without payload/secret logging.
- Audit integration: explicit audit event emission for write authorization decisions is deferred to a future stage to avoid fragile coupling in this rollout.
- Audit failure therefore cannot alter endpoint response in P5-11 because dedicated write-authorization audit emission is not yet enabled.

## Test matrix

- Write pilot disabled preserves existing behavior.
- Write pilot enabled + missing credentials => `401`.
- Write pilot enabled + missing `ProjectsWrite` => `403`.
- Write pilot enabled + `ProjectsWrite` + matching scope => success.
- Building write requires `BuildingsWrite`; `ProjectsWrite` alone is insufficient.
- Tenant mismatch => `403/404` based on `ReturnNotFoundForTenantMismatch`.
- Read pilot behavior remains effective and is not weakened by write pilot.
- Workflow endpoints are not accidentally switched to `401/403` by write pilot rollout flags.

## What remains unprotected

- Full workflow execute authorization rollout.
- Full calculation run authorization rollout.
- Full report generation/write authorization rollout.
- Artifact write/delete authorization rollout.
- Complete tenant-isolation enforcement at query/persistence layers.

## Next rollout candidates

- P5-12 controlled execution rollout for workflow/calculation endpoints (`docs/security/protected-execution-endpoints-rollout.md`).
- Workflow execute endpoints (`WorkflowsExecute`) with staged scope checks.
- Calculation run endpoints (`WorkflowsExecute`) with scoped authorization + rate-limit coordination.
- Report generate/export endpoints (`ReportsWrite`) with read/write separation.
- Artifact management endpoints with explicit ownership/scope enforcement.
