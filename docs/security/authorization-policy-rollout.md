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

## P5-13 protected report/artifact rollout result

- Controlled report/artifact pilot protection is implemented for selected report controllers and workflow report/trace/artifact-read endpoints.
- Rollout enforcement is option-gated by:
  - `ApiAuthorization:Enabled`;
  - `ApiAuthorization:EnableReportArtifactEndpointProtectionPilot`;
  - `ApiAuthorization:RequireReportReadAuthorization`;
  - `ApiAuthorization:RequireReportWriteAuthorization`;
  - `ApiAuthorization:RequireArtifactReadAuthorization`;
  - `ApiAuthorization:RequireArtifactWriteAuthorization`.
- `ReportsRead` is used for report/artifact read access in this stage.
- `ReportsWrite` is used for workflow report generation/export in this stage.
- Public artifact write/delete endpoints are not exposed and remain deferred.
- Detailed rollout notes and test matrix: `docs/security/protected-report-artifact-endpoints-rollout.md`.

## P5-14 protected workflow read/history rollout result

- Controlled workflow read/history pilot protection is implemented for selected read endpoints in `EngineeringWorkflowController`:
  - workflow state read;
  - scenario read/list;
  - job status/events/list.
- Rollout enforcement is option-gated by:
  - `ApiAuthorization:Enabled`;
  - `ApiAuthorization:EnableWorkflowReadEndpointProtectionPilot`;
  - `ApiAuthorization:RequireWorkflowReadAuthorization`.
- Workflow read policy requires `WorkflowsRead` in this stage.
- Anti-enumeration behavior for tenant mismatch uses:
  - `ApiAuthorization:ReturnNotFoundForWorkflowTenantMismatch` (workflow-read specific);
  - fallback to `ApiAuthorization:ReturnNotFoundForTenantMismatch`.
- P5-12 execution (`WorkflowsExecute`) and P5-13 report/artifact policies remain separate and are not downgraded by P5-14.
- Detailed rollout notes and test matrix: `docs/security/protected-workflow-read-history-rollout.md`.

## P5-15 tenant isolation integration matrix result

- P5-15 adds a cross-tenant integration matrix for protected endpoint groups already covered by P5-10 through P5-14.
- Matrix actors use Tenant A (`organizationId=1001`), Tenant B (`organizationId=1002`), anonymous, and limited principals.
- Expected behavior is standardized across protected groups:
  - same tenant with required permission proceeds to success or the endpoint's existing non-authorization status;
  - missing permission returns `403`;
  - anonymous access returns `401`;
  - cross-tenant access returns `403` or `404` according to anti-enumeration options.
- Matrix coverage is documented in `docs/security/tenant-isolation-integration-matrix.md` and machine-readable in `docs/security/tenant-isolation-integration-matrix.json`.
- P5-15 does not add database row-level security, a new identity provider, or a claim that tenant-boundary enforcement is finished.

## P5-16A persistence-backed tenant ownership result

- P5-16A adds nullable `Project.OrganizationId` and `Project.OwnerUserId` fields as a persistence-backed ownership foundation.
- Project/building/workflow scope resolvers can now use persisted Project ownership where metadata is available.
- The migration `AddProjectTenantOwnershipFields` adds nullable columns and indexes only; it performs no data backfill and introduces no destructive operation.
- P5-16A does not add global query filters, database row-level security, new route protection, or external identity provider integration.
- Detailed notes: `docs/security/persistence-backed-tenant-ownership-fields.md`.

## P5-16B tenant-aware query isolation services result

- P5-16B adds explicit tenant query context, query decision, and query isolation policy contracts.
- `IProjectTenantScopedReadService` and `IBuildingTenantScopedReadService` provide foundation-only tenant-scoped read helpers using persisted `Project.OrganizationId`.
- Project list queries exclude other tenants and exclude legacy unscoped projects by default unless explicit transition opt-in is supplied.
- Building queries derive tenant scope from the parent Project and do not duplicate tenant columns.
- Controllers are not switched to these services in this stage, and no public route/API behavior changes are introduced.
- P5-16B does not add global EF query filters, database row-level security, ownership backfill, or external identity provider integration.
- Detailed notes: `docs/security/tenant-aware-query-isolation-services.md`.

## P5-16C tenant-aware read controller integration result

- P5-16C integrates tenant-aware query services into protected Project/Building read controller actions.
- Integration is controlled by existing P5-10 protected-read options and preserves compatibility defaults when protection is disabled.
- No public route or DTO contract changes are introduced.
- Workflow tenant-aware query controller integration remains staged.
- P5-16C does not add global EF query filters, database row-level security, ownership backfill, or external identity provider integration.
- Detailed notes: `docs/security/tenant-aware-read-controller-integration.md`.

## P5-16D workflow tenant-aware read integration result

- P5-16D introduces `IWorkflowTenantScopedReadService` and integrates protected workflow read/history controller actions behind existing P5-14 workflow-read rollout options.
- Workflow state/scenario/job read paths use tenant-aware query decisions based on workflow scope resolver metadata and persisted project ownership fallback where available.
- Cross-tenant anti-enumeration remains `403/404` option-controlled through workflow/global not-found mismatch options.
- Default compatibility remains unchanged when workflow-read rollout options are disabled.
- P5-16D does not add global EF query filters, database row-level security, ownership backfill, or external identity provider integration.
- Detailed notes: `docs/security/workflow-tenant-aware-read-integration.md`.

## P5-17 workflow ownership metadata coverage result

- P5-17 adds explicit workflow/scenario/job ownership metadata inventory documentation in markdown and machine-readable JSON/schema form.
- `DefaultWorkflowAccessScopeResolver` treats invalid/missing ownership identifiers as unresolved scope and prioritizes scenario linkage metadata for job scope derivation.
- Workflow tenant-scoped read strict mode denies unresolved ownership metadata paths; compatibility mode remains explicit option-controlled.
- No ownership backfill is performed in this step.
- P5-17 does not add global EF query filters, database row-level security, or external identity provider integration.
- Detailed notes: `docs/security/workflow-ownership-metadata-coverage.md`.

## Audit relationship

- Authorization decisions should emit audit events through `docs/security/audit-log-foundation.md` when safe.
- Authorization denials should prefer audit references (`AUD-AUTHZ-002`) instead of payload logging.
