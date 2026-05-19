# Persistence-Backed Tenant Ownership Fields

## Purpose

P5-16A introduces a minimal persisted ownership foundation for staged SaaS hardening. It gives project-scoped authorization resolvers a durable source for tenant ownership while preserving legacy compatibility and avoiding route/API behavior changes.

P5-16B builds on this foundation with explicit tenant-aware query isolation services (`docs/security/tenant-aware-query-isolation-services.md`) that use `Project.OrganizationId` without enabling global query filters.

## Scope

This step covers:

- nullable `Project.OrganizationId`;
- nullable `Project.OwnerUserId`;
- EF mapping and indexes for the new project ownership fields;
- an append-only migration named `AddProjectTenantOwnershipFields`;
- project/building/workflow scope resolver usage where safe;
- tests and governance checks for ownership fields and migration safety.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Ownership fields

P5-16A adds transition ownership fields to `Project`:

- `OrganizationId`: nullable integer tenant identifier.
- `OwnerUserId`: nullable integer principal/user identifier.

Both fields are nullable so existing legacy projects remain readable during migration. Assign methods require positive identifiers and reject zero or negative values.

## Project ownership model

Project is the first persisted source of tenant truth:

- tenant-owned project: `OrganizationId` has a positive value;
- owner-associated project: `OwnerUserId` has a positive value;
- legacy/unscoped project: both fields can remain null during migration.

`Project.IsTenantScoped` is true when `OrganizationId` is present. This is intentionally a domain-level signal, not a query filter.

## Building scope derivation

Building does not duplicate tenant ownership columns in P5-16A. `DefaultBuildingReadAccessScopeResolver` resolves the building, then derives tenant scope through the parent project resolver using `Building.ProjectId`.

This keeps the ownership source centralized on Project and avoids a broad schema rewrite.

## Workflow/scenario/job scope derivation

Workflow/scenario/job persistence already carries project/building metadata where available. `DefaultWorkflowAccessScopeResolver` continues to resolve workflow scope through project/building resolvers:

- building metadata can resolve through building scope;
- project metadata can resolve through project scope;
- missing workflow metadata remains staged and unscoped rather than inventing tenant ownership.

## Legacy unscoped project behavior

Legacy projects with null `OrganizationId` remain supported by existing compatibility policy options. Strict policy can deny unscoped resources, while transition policy can allow them when explicitly configured.

P5-16A does not require ownership on project creation, does not require tenant ids in requests, and does not change public DTO shapes.

## Migration details

Migration: `AddProjectTenantOwnershipFields`.

The migration adds:

- nullable `OrganizationId` column on `Projects`;
- nullable `OwnerUserId` column on `Projects`;
- `IX_Projects_OrganizationId`;
- `IX_Projects_OwnerUserId`;
- `IX_Projects_OrganizationId_Id`.

No foreign keys are added in this step to avoid cross-module persistence coupling. No data backfill is performed.

## Resolver behavior

- Project scope now uses persisted `Project.OrganizationId` and `Project.OwnerUserId`.
- Building scope derives `OrganizationId` and `OwnerUserId` from the parent project scope.
- Workflow/scenario/job scope inherits tenant metadata from project/building scope where metadata is present.
- Missing project/building/workflow metadata returns null or staged unscoped scope according to the existing resolver convention.

## Known limitations

- No global query filters are introduced.
- No database row-level security is introduced.
- No persistence-backed organization/user foreign keys are introduced.
- No external identity provider is integrated.
- Existing legacy projects can remain unscoped until a controlled ownership backfill is designed.
- P5-16B query isolation services are foundation-only and are not wired into controllers yet.
- Dedicated artifact write/delete endpoint protection remains deferred until public endpoints exist.

## Next steps

- P5-16B/P6: continue controlled query isolation rollout from `docs/security/tenant-aware-query-isolation-services.md`.
- P6-00: strategy-only ownership backfill planning and evidence model (`docs/security/ownership-backfill-strategy.md`, `docs/security/ownership-backfill-evidence-model.md`).
- Add controlled backfill tooling for legacy project ownership after P6-00 governance gates.
- Add durable organization/user persistence and lifecycle once identity provider integration is selected.
- Add deeper workflow/report/artifact scope tests after metadata coverage is expanded.
