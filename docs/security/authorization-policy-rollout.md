# Authorization Policy Rollout

## Purpose

This document defines the staged authorization policy rollout for AssistantEngineer API without requiring a one-step lock-down of all routes.

## Scope

This rollout covers:

- project access;
- building access;
- workflow access;
- calculation execution;
- report access;
- administration and development endpoints;
- principal mapping from API authentication boundary;
- development compatibility mode;
- regression test strategy.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that route protection is fully rolled out in this step.

## Permission model

Authorization policies are aligned with the Identity permission model:

- `ProjectsRead`
- `ProjectsWrite`
- `BuildingsRead`
- `BuildingsWrite`
- `WorkflowsRead`
- `WorkflowsExecute`
- `ReportsRead`
- `ReportsWrite`
- `AdministrationManage`

## Resource authorization model

- Project authorization must use tenant and ownership-aware project scope.
- Building authorization must resolve project scope inheritance.
- Workflow authorization must resolve project/building scope inheritance.
- Report authorization inherits workflow/project/building ownership boundaries.
- Development or administration endpoints require explicit policy and environment gating.

## Failure model

- Return `401` for unauthenticated access to protected endpoints.
- Return `403` for authenticated principals lacking required permissions.
- `404` can be used for anti-enumeration only when explicitly documented by policy.
- Tenant mismatch details should not leak in response bodies.

## Development compatibility

- Authorization can run in staged compatibility mode while migration is in progress.
- Production target should enable strict protected-route authorization.
- Public endpoints must be explicitly inventoried and reviewed.
- Rate limiting is complementary and separate from authorization decisions (`docs/security/rate-limiting-foundation.md`).

## Rollout stages

- P5-04A: policy foundation and endpoint protection inventory.
- P5-04B: protect low-risk read-only pilot endpoints.
- P5-04C: protect write and execute endpoints.
- P5-04D: add tenant-scoped integration and regression tests.
- P5-04E: enforce OpenAPI and route-level security governance checks.

Frontend readiness note: P5-07 adds protected-content UX foundation (`docs/frontend/frontend-auth-shell-foundation.md`) to represent unauthorized/forbidden states without forcing route lock-down by default.
Route inventory guardrails are tracked in `docs/security/security-regression-guardrails.md` and `docs/security/api-endpoint-protection-inventory.json`.

## P5-09 pilot result

- Controlled pilot protection is implemented for `DevelopmentDemoDataController` (`POST /api/v{version:apiVersion}/development/demo-data/seed`).
- Pilot enforcement is option-gated by `ApiAuthorization:Enabled` and `ApiAuthorization:EnableEndpointProtectionPilot`.
- Pilot policy requires `AdministrationManage` when enabled.
- Development environment gate (`IsDevelopment()`) remains mandatory and unchanged.
- Detailed rollout notes and test matrix: `docs/security/protected-endpoint-pilot-rollout.md`.

## P5-10 protected read rollout result

- Controlled read-only pilot protection is implemented for:
  - `ProjectsController` read endpoints (`GET /projects`, `GET /projects/{id}`);
  - `BuildingsController` read endpoints (`GET /projects/{projectId}/buildings`, `GET /buildings/{id}`).
- Read rollout enforcement is option-gated by:
  - `ApiAuthorization:Enabled`;
  - `ApiAuthorization:EnableReadEndpointProtectionPilot`;
  - `ApiAuthorization:RequireProjectReadAuthorization` and `ApiAuthorization:RequireBuildingReadAuthorization`.
- Tenant mismatch response behavior is controlled by `ApiAuthorization:ReturnNotFoundForTenantMismatch`.
- Detailed rollout notes and test matrix: `docs/security/protected-read-endpoints-rollout.md`.

## P5-11 protected write rollout result

- Controlled write-endpoint pilot protection is implemented for selected `ProjectsController` and `BuildingsController` create/update/delete actions.
- Write rollout enforcement is option-gated by:
  - `ApiAuthorization:Enabled`;
  - `ApiAuthorization:EnableWriteEndpointProtectionPilot`;
  - `ApiAuthorization:RequireProjectWriteAuthorization` and `ApiAuthorization:RequireBuildingWriteAuthorization`.
- Project write policy requires `ProjectsWrite` for create/update/delete (with scoped checks on route-bound resources).
- Building write policy requires `BuildingsWrite` for create/update/delete (including parent project scope checks for create routes).
- Workflow/calculation/report execute endpoints remain outside P5-11 scope and unchanged by this rollout.
- Detailed rollout notes and test matrix: `docs/security/protected-write-endpoints-rollout.md`.

## P5-12 protected execution rollout result

- Controlled execution-endpoint pilot protection is implemented for selected workflow execute and calculation run routes:
  - `EngineeringWorkflowController` execution routes (`prepare-calculation`, `run-calculation`, `jobs`, `jobs/{jobId}/cancel`);
  - load-calculation controllers for building/floor/room execution endpoints.
- Execution rollout enforcement is option-gated by:
  - `ApiAuthorization:Enabled`;
  - `ApiAuthorization:EnableExecutionEndpointProtectionPilot`;
  - `ApiAuthorization:RequireWorkflowExecuteAuthorization` and `ApiAuthorization:RequireCalculationRunAuthorization`.
- Execution policy requires `WorkflowsExecute` in this stage.
- Report/artifact endpoint groups remain outside P5-12 scope.
- Detailed rollout notes and test matrix: `docs/security/protected-execution-endpoints-rollout.md`.

## Audit relationship

- Authorization decisions should emit audit events through `docs/security/audit-log-foundation.md` when safe.
- Authorization denials should prefer audit references (`AUD-AUTHZ-002`) instead of payload logging.
