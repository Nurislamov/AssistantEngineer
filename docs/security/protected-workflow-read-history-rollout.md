# Protected Workflow Read/History Rollout

## Purpose

This document records P5-14 controlled rollout of workflow read/history endpoint protection without global route lock-down.

P5-16D tenant-aware workflow query/controller integration builds on this rollout: `docs/security/workflow-tenant-aware-read-integration.md`.
P5-17 metadata coverage hardening for workflow/scenario/job ownership is documented in `docs/security/workflow-ownership-metadata-coverage.md`.

## Scope

This rollout covers selected workflow read/history routes:

- workflow state read (`GET /engineering-workflow/{projectId}/state`);
- scenario read/list (`GET /engineering-workflow/scenarios/{scenarioId}`, `GET /engineering-workflow/{projectId}/scenarios`);
- job status/read/list (`GET /engineering-workflow/jobs/{jobId}`, `GET /engineering-workflow/jobs/{jobId}/events`, `GET /engineering-workflow/{projectId}/jobs`).

Execution endpoints remain in P5-12 and report/artifact endpoints remain in P5-13.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.
- No claim that workflow tenant isolation is complete unless resolver scope is proven.

## Selected endpoint groups

- `EngineeringWorkflowController` read/history routes:
  - `GetWorkflowState`
  - `GetScenarioResult`
  - `GetProjectScenarios`
  - `GetJob`
  - `GetJobEvents`
  - `ListProjectJobs`

## Workflow read policy

- Required permission: `WorkflowsRead`.
- Applied only when:
  - `ApiAuthorization:Enabled=true`;
  - `ApiAuthorization:EnableWorkflowReadEndpointProtectionPilot=true`;
  - `ApiAuthorization:RequireWorkflowReadAuthorization=true`.

## Scenario read policy

- `GET /engineering-workflow/scenarios/{scenarioId}` and `GET /engineering-workflow/{projectId}/scenarios` require `WorkflowsRead` in pilot mode.
- Scenario route checks use scenario id + resolved project/building scope where available.

## Job status/read policy

- `GET /engineering-workflow/jobs/{jobId}`, `GET /engineering-workflow/jobs/{jobId}/events`, and `GET /engineering-workflow/{projectId}/jobs` require `WorkflowsRead` in pilot mode.
- Job route checks use job id plus project fallback scope when metadata is available.

## Resource scope resolution

- `RequireWorkflowReadPermissionAsync` supports workflow/scenario/job identifiers plus project/building fallback.
- `DefaultWorkflowAccessScopeResolver` now resolves:
  - workflow id via scenario-id/job-id lookup when available;
  - scenario id via workflow persistence metadata;
  - job id via job repository metadata and scenario linkage.
- If tenant metadata cannot be proven, staged fallback continues through project/building resolvers.
- This rollout adds route-level authorization checks, not full query-level tenant isolation.

## Anti-enumeration behavior

- Unauthenticated caller: `401`.
- Authenticated without `WorkflowsRead`: `403`.
- Tenant mismatch:
  - `404` when `ReturnNotFoundForTenantMismatch=true` or `ReturnNotFoundForWorkflowTenantMismatch=true`;
  - `403` otherwise.
- Missing workflow/scenario/job resources keep existing not-found behavior.
- Responses avoid tenant ownership detail leakage.

## Compatibility defaults

Compatibility-safe defaults remain:

- `ApiAuthorization:Enabled=false`;
- `ApiAuthorization:EnableWorkflowReadEndpointProtectionPilot=false`;
- `ApiAuthorization:RequireWorkflowReadAuthorization=false`;
- `ApiAuthorization:ReturnNotFoundForWorkflowTenantMismatch=false`;
- `ApiAuthorization:AllowAnonymousInDevelopment=true`.

## Tenant mismatch behavior

- Workflow read/history authorization uses `ReturnNotFoundForWorkflowTenantMismatch` with fallback to `ReturnNotFoundForTenantMismatch`.
- This allows anti-enumeration tuning for workflow read routes without changing default compatibility behavior.

## Rate limiting relationship

- Workflow read/history routes align with `WorkflowRead` category governance in `docs/security/rate-limiting-policy-registry.json`.
- This rollout does not enable global or breaking rate limits by default.

## Audit/observability behavior

- Authorization gate continues structured deny logs without payload/secret logging.
- Explicit workflow read denial audit event emission remains staged future work to avoid coupling regressions in this step.
- Audit failure therefore cannot alter endpoint results in this rollout.

## Test matrix

- Workflow-read pilot disabled preserves existing behavior.
- Workflow-read pilot enabled + no credentials => `401`.
- Workflow-read pilot enabled + missing `WorkflowsRead` => `403`.
- Workflow-read pilot enabled + matching `WorkflowsRead` scope => success path.
- Tenant mismatch respects `403/404` anti-enumeration option.
- Scenario/job missing resources preserve existing not-found behavior.
- P5-12 execution endpoints still require `WorkflowsExecute`.
- P5-13 report/artifact endpoints keep `ReportsRead`/`ReportsWrite` policies.

## What remains unprotected

- Dedicated artifact write/delete API protection (no public endpoints exposed yet).
- Full workflow history query-layer tenant filtering guarantees.
- Full tenant-isolation integration matrix across every repository/query path.
- Dedicated workflow read denial audit events wired into durable audit storage.

## Next rollout candidates

- Dedicated artifact write/delete endpoint protection if/when API exposure is introduced.
- Deeper tenant isolation integration tests across scenario/job/artifact persistence boundaries.
- Optional durable audit emission for workflow read authorization denials.
- Organization-plan specific workflow-read rate-limit tuning once distributed rate limiting is introduced.

P5-15 tenant isolation note:

- Cross-tenant expectations for `WorkflowsRead`, scenario read, job read, and job events read paths are tracked in `docs/security/tenant-isolation-integration-matrix.md`.
- P5-15 adds anti-enumeration checks for `403` versus `404` tenant mismatch behavior without changing workflow read defaults.
