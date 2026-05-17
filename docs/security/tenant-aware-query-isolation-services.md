# Tenant-Aware Query Isolation Services

## Purpose

P5-16B introduces explicit tenant-aware query isolation services for staged SaaS hardening. The goal is to prove project/building read queries can exclude cross-tenant resources using persisted project ownership fields without enabling global EF query filters or changing public API behavior.

## Scope

This step covers:

- `TenantQueryContext` and `TenantScopedQueryDecision` contracts;
- `TenantQueryIsolationPolicy` for explicit read/write resource decisions;
- project tenant-scoped read helpers;
- building tenant-scoped read helpers deriving tenant ownership from parent project;
- unit and matrix tests for same-tenant, cross-tenant, missing-permission, and legacy-unscoped behavior;
- documentation and governance checks.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Query isolation model

P5-16B uses explicit services rather than implicit global filtering. Callers must opt into tenant-scoped read helpers and pass a `TenantQueryContext` built from an authenticated principal or test principal.

The policy treats `OrganizationId` as the primary tenant boundary. A resource with a persisted organization id is readable only when the query context has the same organization id and the required permission.

## TenantQueryContext

`TenantQueryContext` captures:

- `UserId`;
- `OrganizationId`;
- `IsAuthenticated`;
- permission names;
- `AllowUnscopedResourcesDuringTransition`;
- `StrictTenantMatch`;
- `ReturnNotFoundForTenantMismatch`;
- `IncludeUnscopedResourcesInTenantLists`.

`OrganizationId = null` means tenant scope is unavailable. It does not grant access to scoped resources.

## TenantQueryIsolationPolicy

`TenantQueryIsolationPolicy` evaluates explicit resource access:

- unauthenticated context is denied as `Unauthenticated`;
- authenticated context missing the required permission is denied as `MissingPermission`;
- matching `OrganizationId` is allowed as tenant-scoped;
- mismatched `OrganizationId` is denied as `TenantMismatch`;
- missing principal organization for a scoped resource is denied as `MissingOrganization`;
- null resource organization is allowed only when legacy unscoped resources are explicitly allowed.

Tenant mismatch can request not-found behavior through `ReturnNotFoundForTenantMismatch` for anti-enumeration.

## Project tenant-scoped reads

`IProjectTenantScopedReadService` adds explicit methods for:

- getting one project for a tenant;
- listing projects for a tenant.

Single-project reads use `Project.OrganizationId` and require `ProjectsRead`. Tenant lists return only projects with the caller organization id. Legacy unscoped projects are excluded from tenant lists by default and included only when explicitly requested.

## Building tenant-scoped reads

`IBuildingTenantScopedReadService` adds explicit methods for:

- getting one building for a tenant;
- listing buildings for a project after checking the parent project tenant boundary.

Building ownership is derived from the parent `Project.OrganizationId`; P5-16B does not duplicate organization fields onto `Building`.

## Workflow tenant-scoped reads

P5-16D introduces `IWorkflowTenantScopedReadService` and controlled workflow read/history controller integration (`docs/security/workflow-tenant-aware-read-integration.md`).

Workflow query resolution uses existing workflow scope resolver metadata and falls back to persisted project ownership scope when available. Metadata-incomplete paths remain staged and compatibility-option controlled.
P5-17 hardens metadata coverage inventory and resolver behavior details in `docs/security/workflow-ownership-metadata-coverage.md`.

## Legacy unscoped resources

Legacy projects can still have `OrganizationId = null` during migration. P5-16B keeps this behavior explicit:

- single-resource reads can allow legacy unscoped records when `AllowUnscopedResourcesDuringTransition = true`;
- single-resource reads deny legacy unscoped records when transition compatibility is disabled;
- tenant list queries do not include legacy unscoped records by default;
- tenant list queries include legacy unscoped records only with explicit opt-in.

## Compatibility defaults

P5-16B introduced the services as a foundation. P5-16C integrates Project/Building protected read controllers (`docs/security/tenant-aware-read-controller-integration.md`). P5-16D extends this integration to protected workflow read/history controllers (`docs/security/workflow-tenant-aware-read-integration.md`).

Default local/development compatibility is preserved because rollout options remain disabled by default. No auth or tenant query filtering is enabled globally by default.

## Relationship to authorization gates

Authorization gates answer whether a protected endpoint may proceed. Tenant-aware query services answer whether a read query should return a resource after a tenant-aware lookup.

Query isolation complements route authorization; it does not replace authentication or endpoint permission checks.

## Relationship to persisted ownership fields

The services use the P5-16A persisted project ownership fields:

- `Project.OrganizationId`;
- `Project.OwnerUserId`.

Building queries derive tenant scope from the parent project. Workflow/report/artifact scope can inherit project ownership where metadata points back to project/building records, but P5-16B does not expand workflow/report/artifact persistence metadata.

## Why global EF query filters are not enabled yet

Global `HasQueryFilter` is intentionally not enabled because:

- legacy unscoped resources still exist until a backfill strategy is completed;
- route/controller integration needs a staged rollout and stronger regression coverage;
- DbContext should not silently depend on ambient current-user state yet;
- explicit services are easier to test and reason about before broad repository integration.

## Known limitations

- Workflow metadata coverage is still partially staged for some historical/incomplete paths; see `docs/security/workflow-ownership-metadata-coverage.md`.
- Global EF query filters are not enabled.
- Database row-level security is not implemented.
- Ownership backfill is not implemented.
- External identity provider integration is not implemented.
- Artifact write/delete API protection remains deferred until endpoints exist.

## Next steps

- P5-17: harden workflow/scenario/job metadata ownership completeness and reduce staged fallback paths.
- Add ownership backfill tooling and migration runbook for legacy unscoped projects.
- Consider global query filters only after backfill, controller integration, and regression coverage are ready.
