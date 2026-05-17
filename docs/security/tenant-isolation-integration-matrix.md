# Tenant Isolation Integration Matrix

## Purpose

This document records the cross-tenant integration test matrix for the staged protected endpoint rollout. It gives P5-15 a shared source of truth for tenant actors, representative resources, expected anti-enumeration behavior, and the automated tests that exercise the already protected endpoint groups.

## Scope

The matrix covers:

- Project read/write endpoint protection.
- Building read/write endpoint protection.
- Workflow execute endpoint protection.
- Calculation execute endpoint protection.
- Report read/generate/export endpoint protection.
- Artifact read endpoint protection where exposed through existing workflow/report routes.
- Workflow state, scenario, job, and history read endpoint protection.
- Anti-enumeration behavior for cross-tenant access.
- Compatibility/default-disabled behavior for staged rollout options.
- Persistence-backed Project ownership source introduced in `docs/security/persistence-backed-tenant-ownership-fields.md`.
- P5-16B query isolation service matrix introduced in `docs/security/tenant-aware-query-isolation-services.md`.
- P5-16C protected Project/Building read controller integration introduced in `docs/security/tenant-aware-read-controller-integration.md`.
- P5-16D protected workflow read/history tenant-aware integration introduced in `docs/security/workflow-tenant-aware-read-integration.md`.
- P5-17 workflow/scenario/job metadata coverage inventory introduced in `docs/security/workflow-ownership-metadata-coverage.md`.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Tenant test actors

Tenant A:

- `organizationId = 1001`
- `userId = 2001`
- permissions: `ProjectsRead`, `ProjectsWrite`, `BuildingsRead`, `BuildingsWrite`, `WorkflowsRead`, `WorkflowsExecute`, `ReportsRead`, `ReportsWrite`

Tenant B:

- `organizationId = 1002`
- `userId = 2002`
- same permissions as Tenant A

Anonymous:

- no authenticated principal

Limited principal:

- `organizationId = 1001`
- permissions selected per test so the required permission is absent

## Resource matrix

- `ProjectA` belongs to Tenant A.
- `BuildingA` belongs to `ProjectA` and Tenant A.
- `WorkflowA` belongs to `ProjectA`, `BuildingA`, and Tenant A where workflow metadata is available.
- `ScenarioA` belongs to `WorkflowA` and Tenant A where scenario metadata is available.
- `JobA` belongs to `WorkflowA` and Tenant A where job metadata is available.
- `ReportA` belongs to `WorkflowA` or `BuildingA` and Tenant A where report scope can be resolved.
- `ArtifactA` belongs to `WorkflowA` or `ReportA` and Tenant A where artifact scope can be resolved.
- Equivalent Tenant B resources are represented by the same resource identifiers with Tenant B scope in tests.

P5-16A note: project ownership can now be represented by persisted nullable `Project.OrganizationId` and `Project.OwnerUserId` fields. Building and workflow scopes derive tenant ownership through Project where route metadata makes that safe.

P5-16B/P5-16C/P5-16D/P5-17 note: explicit tenant-scoped read services now cover Project/Building and controlled workflow read/history query paths. Metadata coverage for workflow groups is now explicitly tracked as complete/partial in machine-readable inventory documents.

## Expected behavior matrix

For each protected group:

- Same tenant plus required permission: success or the endpoint's existing non-authorization status.
- Same tenant plus missing permission: `403 Forbidden`.
- Cross tenant plus required permission: `403 Forbidden` when `ReturnNotFoundForTenantMismatch=false`.
- Cross tenant plus required permission: `404 NotFound` when `ReturnNotFoundForTenantMismatch=true`.
- Anonymous principal: `401 Unauthorized` when the relevant protection pilot is enabled.
- Missing resource: existing `404 NotFound` behavior is preserved where the gate can resolve the resource; route/controller-level not-found remains authoritative for workflow identifiers whose metadata is not present.
- Protection disabled: existing compatibility behavior is preserved by the P5-10 through P5-14 rollout tests.

## Endpoint groups

| Group | Permission | Rollout stage | Expected same-tenant result | Cross-tenant result |
| --- | --- | --- | --- | --- |
| ProjectsRead | ProjectsRead | P5-10 + P5-16C | Success/existing endpoint status | 403 or 404 by option |
| ProjectsWrite | ProjectsWrite | P5-11 | Success/existing endpoint status | 403 or 404 by option |
| BuildingsRead | BuildingsRead | P5-10 + P5-16C | Success/existing endpoint status | 403 or 404 by option |
| BuildingsWrite | BuildingsWrite | P5-11 | Success/existing endpoint status | 403 or 404 by option |
| WorkflowsRead | WorkflowsRead | P5-14 + P5-16D | Success/existing endpoint status | 403 or 404 by option |
| WorkflowsExecute | WorkflowsExecute | P5-12 | Success/existing endpoint status | 403 or 404 by option |
| CalculationRun | WorkflowsExecute | P5-12 | Success/existing endpoint status | 403 or 404 by option |
| ReportsRead | ReportsRead | P5-13 | Success/existing endpoint status | 403 or 404 by option |
| ReportsWrite | ReportsWrite | P5-13 | Success/existing endpoint status | 403 or 404 by option |
| ArtifactRead | ReportsRead | P5-13 | Success/existing endpoint status | 403 or 404 by option |
| WorkflowScenarioRead | WorkflowsRead | P5-14 + P5-16D | Success/existing endpoint status | 403 or 404 by option |
| WorkflowJobRead | WorkflowsRead | P5-14 + P5-16D | Success/existing endpoint status | 403 or 404 by option |
| WorkflowJobEventsRead | WorkflowsRead | P5-14 + P5-16D | Success/existing endpoint status | 403 or 404 by option |
| ProjectTenantScopedReadService | ProjectsRead | P5-16B | Success/service success | Failure or not-found by option |
| BuildingTenantScopedReadService | BuildingsRead | P5-16B | Success/service success | Failure or not-found by option |
| WorkflowTenantScopedReadService | WorkflowsRead | P5-16D | Success/service success | Failure or not-found by option |

## Known limitations

- Full database row-level security is not implemented.
- Dedicated public artifact write/delete endpoints are absent/deferred; P5-15 does not add new artifact endpoints.
- Dedicated `ArtifactRead`/`ArtifactWrite` permissions are not introduced; existing report-artifact routes use `ReportsRead`/`ReportsWrite`.
- Some workflow/report/artifact scope decisions use staged fallback when durable metadata does not yet contain complete tenant scope.
- Endpoint protection remains options-controlled and default compatibility remains preserved.
- P5-15 tests harden the staged rollout while tenant-boundary enforcement remains future work.
- P5-16A adds Project ownership fields, but global query filters and database row-level security remain future work.
- Project/Building protected read controller integration uses tenant-aware query services only when protected-read rollout is enabled.
- Workflow tenant-aware read integration is enabled for protected workflow read/history paths, but metadata-incomplete workflow/scenario/job ownership resolution remains staged and compatibility-option controlled.
- Workflow metadata coverage is not uniformly complete across all legacy paths; see `docs/security/workflow-ownership-metadata-coverage.md`.
