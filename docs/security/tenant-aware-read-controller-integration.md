# Tenant-Aware Read Controller Integration

## Purpose

P5-16C integrates tenant-aware query services into already protected Project and Building read controllers. This step validates controller-level tenant-scoped query behavior without changing public routes, DTO shapes, or default compatibility mode.

## Scope

P5-16C covers:

- `ProjectsController` protected read actions using `IProjectTenantScopedReadService`;
- `BuildingsController` protected read actions using `IBuildingTenantScopedReadService`;
- tenant query context mapping from authenticated principal and staged options;
- protected-read integration tests for same-tenant, cross-tenant, anonymous, missing-permission, list filtering, and legacy-unscoped behavior;
- governance/docs updates for matrix and readiness inventory.

P5-16C does not enable global query filters, database row-level security, workflow tenant query integration, ownership backfill, or external identity provider integration.

P5-16D extends this controller-integration pattern to protected workflow read/history routes in `docs/security/workflow-tenant-aware-read-integration.md`.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Integrated endpoint groups

- `ProjectsRead` (P5-10 protected read rollout group) now uses tenant-aware query service path when protected-read rollout options are enabled.
- `BuildingsRead` (P5-10 protected read rollout group) now uses tenant-aware query service path when protected-read rollout options are enabled.

## Project read integration

Integrated actions:

- `GET /api/v{version}/projects`
- `GET /api/v{version}/projects/{id}`

Behavior:

- protection disabled: existing `IBuildingsFacade` path is preserved;
- protection enabled: controller builds `TenantQueryContext` and uses `ProjectTenantScopedReadService`;
- same-tenant plus `ProjectsRead`: existing success response shape;
- cross-tenant: `403` or `404` by `ApiAuthorization:ReturnNotFoundForTenantMismatch`;
- anonymous: `401`;
- missing permission: `403`;
- missing resource: existing `404`.

## Building read integration

Integrated actions:

- `GET /api/v{version}/buildings/{id}`
- `GET /api/v{version}/projects/{projectId}/buildings`

Behavior:

- protection disabled: existing `IBuildingsFacade` path is preserved;
- protection enabled: controller builds `TenantQueryContext` and uses `BuildingTenantScopedReadService`;
- building tenant scope is derived from parent `Project.OrganizationId`;
- same-tenant plus `BuildingsRead`: existing success response shape;
- cross-tenant: `403` or `404` by `ApiAuthorization:ReturnNotFoundForTenantMismatch`;
- anonymous: `401`;
- missing permission: `403`;
- missing resource: existing `404`.

## Compatibility defaults

- `ApiAuthorization:Enabled` remains `false` by default in `appsettings.json` and `appsettings.Development.json`.
- Protected-read rollout remains options-controlled (`EnableReadEndpointProtectionPilot`, `RequireProjectReadAuthorization`, `RequireBuildingReadAuthorization`).
- No public route changes and no DTO shape changes are introduced.

## Legacy unscoped behavior

- `Identity:ProjectTenantAccess:AllowUnscopedProjectsDuringTransition` controls compatibility for legacy unscoped Projects (`OrganizationId = null`).
- Project list includes legacy unscoped Projects only when this explicit transition option allows it.
- Building list under legacy unscoped Project follows the same transition option through project-scope checks.

## Anti-enumeration behavior

- Cross-tenant denial remains `403` when `ApiAuthorization:ReturnNotFoundForTenantMismatch=false`.
- Cross-tenant denial returns `404` when `ApiAuthorization:ReturnNotFoundForTenantMismatch=true`.
- Responses do not expose tenant mismatch internals or secret payload details.

## Relationship to authorization gates

- Authorization gate remains the first boundary for authn/authz checks.
- Tenant-aware query service integration is a controlled read-query boundary behind the gate.
- Query services complement gate checks; they do not replace authentication or permission rollout controls.

## Relationship to tenant-aware query services

- P5-16B introduced `TenantQueryContext`, `TenantQueryIsolationPolicy`, `IProjectTenantScopedReadService`, and `IBuildingTenantScopedReadService`.
- P5-16C wires those services into protected Project/Building read controllers.
- Workflow tenant-aware query integration is covered by P5-16D in `docs/security/workflow-tenant-aware-read-integration.md`.

## What remains staged

- Workflow metadata ownership coverage remains partially staged for some historical/incomplete workflow paths.
- Ownership backfill for legacy unscoped Projects remains staged.
- Global EF query filters are not enabled.
- Database row-level security is not implemented.
- External identity provider integration is not implemented.

## Next steps

- P5-17: harden workflow/scenario/job ownership metadata coverage (`docs/security/workflow-ownership-metadata-coverage.md`).
- P6: plan ownership backfill execution and validation.
- P6: evaluate persistence-layer query enforcement options only after backfill and expanded integration coverage.
