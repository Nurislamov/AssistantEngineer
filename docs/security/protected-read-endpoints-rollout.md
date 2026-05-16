# Protected Read Endpoints Rollout

## Purpose

This document records P5-10 controlled rollout of read-only endpoint protection for Projects/Buildings without broad write/execute lock-down.

## Scope

This rollout covers:

- `GET /api/v{version:apiVersion}/projects`;
- `GET /api/v{version:apiVersion}/projects/{id:int}`;
- `GET /api/v{version:apiVersion}/projects/{projectId:int}/buildings`;
- `GET /api/v{version:apiVersion}/buildings/{id:int}`;
- option-gated authorization behavior and compatibility defaults;
- integration and unit test coverage for 401/403/404/success outcomes.

Write/execute endpoints are explicitly out of scope in P5-10.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.

## Selected endpoint groups

- `ProjectsController` read-only endpoints:
  - `GET /projects`
  - `GET /projects/{id}`
- `BuildingsController` read-only endpoints:
  - `GET /projects/{projectId}/buildings`
  - `GET /buildings/{id}`

## Protection behavior

Read protection is enforced only when all required options are enabled:

- `ApiAuthorization:Enabled=true`
- `ApiAuthorization:EnableReadEndpointProtectionPilot=true`
- endpoint-group flag:
  - `ApiAuthorization:RequireProjectReadAuthorization=true` for project reads;
  - `ApiAuthorization:RequireBuildingReadAuthorization=true` for building reads.

When protection is active:

- unauthenticated principal => `401`;
- authenticated principal without required permission => `403`;
- authenticated principal with permission but tenant mismatch => `403` or `404` (option-controlled);
- authenticated principal with required permission and allowed scope => existing endpoint success behavior.

## Compatibility defaults

Compatibility-safe defaults remain:

- `ApiAuthorization:Enabled=false`
- `ApiAuthorization:EnableReadEndpointProtectionPilot=false`
- `ApiAuthorization:RequireProjectReadAuthorization=false`
- `ApiAuthorization:RequireBuildingReadAuthorization=false`
- `ApiAuthorization:ReturnNotFoundForTenantMismatch=false`
- `ApiAuthorization:AllowAnonymousInDevelopment=true`

These defaults preserve local/dev workflow behavior unless rollout options are explicitly enabled.

## Project read policy

- Required permission: `ProjectsRead`.
- `GET /projects/{id}` uses project scope resolution plus `ProjectTenantAccessPolicy`.
- `GET /projects` currently enforces permission gate only (no per-item query filtering in this stage).

## Building read policy

- Required permission: `BuildingsRead`.
- `GET /buildings/{id}` uses building scope resolution plus `ProjectTenantAccessPolicy`.
- `GET /projects/{projectId}/buildings` uses project-scope gate before listing buildings.

## Tenant mismatch behavior

- `ApiAuthorization:ReturnNotFoundForTenantMismatch=false` => mismatch returns `403`.
- `ApiAuthorization:ReturnNotFoundForTenantMismatch=true` => mismatch returns `404`.
- Responses do not include tenant mismatch internals.

## Test matrix

- Read protection disabled preserves existing behavior.
- Read protection enabled without credentials returns `401`.
- Read protection enabled with authenticated principal missing `ProjectsRead` returns `403`.
- Read protection enabled with `ProjectsRead` and matching organization succeeds.
- Tenant mismatch returns `403` or `404` according to option.
- Building read endpoints require `BuildingsRead`.
- Write endpoints are not protected by this read-only rollout step.
- Failure responses are scanned for secret/auth-internal leakage.

## What remains unprotected

- Project write endpoints (`POST/PUT/DELETE`) remain out of this rollout.
- Building write endpoints remain out of this rollout.
- Workflow execute, calculation run, and report generation protection are not expanded in P5-10.
- Full tenant-isolation query filtering is not implemented in this stage.

## Next rollout candidates

- Floors/Rooms read endpoints with building/project scope inheritance.
- Reports read endpoints (`ReportsRead`) under scope-aware access.
- Project/building write endpoints in a separate controlled rollout (post-read stabilization).
- Workflow/calculation execute endpoints with stronger rate-limit and audit integration.

P5-11 follow-up note:

- P5-11 introduces controlled write-endpoint rollout for selected `Projects`/`Buildings` create/update/delete actions.
- See `docs/security/protected-write-endpoints-rollout.md` for write-policy behavior and test coverage.
