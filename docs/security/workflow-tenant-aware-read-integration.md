# Workflow Tenant-Aware Read Integration

## Purpose

P5-16D adds controlled tenant-aware query integration for protected workflow read/history endpoints, reusing persisted project/building metadata and existing workflow scope resolution where available.

## Scope

P5-16D covers:

- workflow state read (`GET /engineering-workflow/{projectId}/state`);
- scenario read/list (`GET /engineering-workflow/scenarios/{scenarioId}`, `GET /engineering-workflow/{projectId}/scenarios`);
- job read/events/list (`GET /engineering-workflow/jobs/{jobId}`, `GET /engineering-workflow/jobs/{jobId}/events`, `GET /engineering-workflow/{projectId}/jobs`);
- `IWorkflowTenantScopedReadService` integration behind existing workflow-read rollout options;
- service/controller tests for same-tenant, cross-tenant, anti-enumeration, and project-scoped list filtering;
- governance updates for docs, matrix metadata, and readiness inventory.

P5-16D does not change routes or DTO contracts and does not enable global filters or database row-level security.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Integrated endpoint groups

- `WorkflowsRead`
- `WorkflowScenarioRead`
- `WorkflowJobRead`
- `WorkflowJobEventsRead`

These groups use tenant-aware workflow read service path when workflow-read protection rollout options are enabled.

## Workflow state read integration

Integrated action:

- `GET /api/v{version}/engineering-workflow/{projectId}/state`

Behavior:

- protection disabled: existing behavior is preserved;
- protection enabled: controller builds tenant query context and uses `IWorkflowTenantScopedReadService`;
- same-tenant + `WorkflowsRead`: existing success response;
- cross-tenant: `403` or `404` by anti-enumeration options;
- missing permission: `403`;
- anonymous: `401`;
- missing project scope: existing not-found behavior.

## Scenario read/list integration

Integrated actions:

- `GET /api/v{version}/engineering-workflow/scenarios/{scenarioId}`
- `GET /api/v{version}/engineering-workflow/{projectId}/scenarios`

Behavior:

- project-scoped list verifies project tenant access first, then returns scenarios only for that project;
- scenario-by-id uses workflow scope resolver and falls back to project ownership metadata when workflow scope metadata is missing;
- cross-tenant reads return `403` or `404` by anti-enumeration options without leaking tenant mismatch details.

## Job read/events/list integration

Integrated actions:

- `GET /api/v{version}/engineering-workflow/jobs/{jobId}`
- `GET /api/v{version}/engineering-workflow/jobs/{jobId}/events`
- `GET /api/v{version}/engineering-workflow/{projectId}/jobs`

Behavior:

- job/event reads authorize job scope first and do not disclose cross-tenant job existence when not-found anti-enumeration is enabled;
- project-scoped job list verifies project tenant access first and returns jobs only for that project;
- protected-mode permission requirement remains `WorkflowsRead`.

## Metadata ownership resolution

Workflow tenant-aware read resolution uses:

- persisted Project ownership (`Project.OrganizationId`) via project scope resolver;
- workflow/scenario/job scope resolution via `IWorkflowAccessScopeResolver`;
- workflow persistence scenario metadata (`Scenario.ProjectId`, `Scenario.BuildingId`);
- job metadata (`Job.ProjectId`, `Job.ScenarioId`) where available.

No hidden ownership is invented when metadata cannot be resolved.

## Anti-enumeration behavior

- anonymous caller: `401`;
- authenticated caller without `WorkflowsRead`: `403`;
- cross-tenant mismatch:
  - `403` when `ApiAuthorization:ReturnNotFoundForWorkflowTenantMismatch=false` and `ApiAuthorization:ReturnNotFoundForTenantMismatch=false`;
  - `404` when workflow/global not-found mismatch option is enabled;
- missing real resources keep existing `404` behavior.

Responses avoid tenant/organization mismatch payload details.

## Compatibility defaults

- `ApiAuthorization:Enabled` remains `false` by default.
- `EnableWorkflowReadEndpointProtectionPilot` and `RequireWorkflowReadAuthorization` remain default-disabled.
- When workflow-read protection is disabled, controller behavior stays compatible with prior flow.

## Relationship to protected workflow read rollout

P5-14 introduced protected workflow read/history authorization gate checks.  
P5-16D adds explicit tenant-aware workflow query service integration behind those same rollout gates for read/history query paths.

## Relationship to tenant-aware query services

- P5-16B introduced query-isolation contracts and Project/Building tenant-scoped services.
- P5-16C integrated Project/Building protected read controllers.
- P5-16D extends the same controller-integration model to workflow read/history routes with `IWorkflowTenantScopedReadService`.

## Staged limitations

- Workflow metadata paths that cannot resolve ownership still use staged safe behavior:
  - strict mode denies when tenant scope cannot be proven;
  - compatibility fallback can allow unscoped transition behavior only when explicit options permit.
- No global EF query filters.
- No database row-level security.
- No ownership backfill.
- No external identity provider integration.
- Report/artifact query paths are not switched by this step.

## Next steps

- P5-17: harden workflow/scenario/job metadata completeness and backfill strategy.
- P6: ownership backfill execution plan and validation.
- P6: evaluate persistence-layer/global query enforcement only after metadata and backfill readiness is proven.
