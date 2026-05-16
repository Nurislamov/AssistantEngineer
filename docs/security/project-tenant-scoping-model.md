# Project Tenant Scoping Model

## Purpose

This document defines the P5-02 tenant/project ownership and scoping foundation for AssistantEngineer. It introduces access-scope contracts and policy rules that future authorization layers can enforce without changing current API route behavior.

## Scope

P5-02 covers:

- project ownership/scoping contracts;
- building/workflow scope inheritance rules;
- principal-to-resource access policy rules;
- migration handling for legacy unscoped resources.

This step does not enable route authorization enforcement.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No route authorization enforcement claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Resource ownership hierarchy

Target hierarchy:

Organization/Tenant
  -> Project
    -> Building
      -> Floor/Room
    -> EngineeringWorkflow
    -> Reports
    -> Artifacts

## Project ownership model

- `ProjectAccessScope` represents project-level ownership metadata for policy evaluation.
- Ownership metadata is modeled as optional in this phase to support legacy datasets.
- Policy checks use principal context, required permission, tenant identifier, and legacy options.
- Principal context is supplied by the API authentication boundary defined in `docs/security/api-authentication-boundary.md`.

## Building scoping model

- `BuildingAccessScope` carries `BuildingId`, `ProjectId`, and optional ownership metadata.
- Building access is evaluated with the same tenant/permission rules used for projects.
- Buildings conceptually inherit tenant boundary from project ownership.

## Workflow scoping model

- `WorkflowAccessScope` carries workflow identity and optional `ProjectId`/`BuildingId` mapping.
- Workflow access follows the same tenant and permission checks.
- Workflow resources remain policy-addressable even before route-level enforcement.

## Legacy unscoped resource migration

- Legacy resources can have missing tenant ownership metadata during migration.
- `ProjectTenantAccessOptions.AllowUnscopedProjectsDuringTransition` controls transitional compatibility.
- `TreatMissingTenantAsBlocking` controls whether missing tenant metadata is immediately denied.
- Production target is strict tenant ownership with legacy compatibility disabled.

## Access policy rules

`ProjectTenantAccessPolicy` rules:

- unauthenticated principal => deny;
- missing required permission => deny;
- inactive tenant scope => deny;
- strict tenant matching compares `PrincipalAccessContext.OrganizationId` to resource `OrganizationId`;
- missing tenant metadata follows transition options;
- no controller/route attributes are evaluated at this layer.

## Schema status

- P5-02 introduces policy/contracts only.
- No Project/Building database schema changes are applied in this step.
- Ownership columns and migration changes are intentionally deferred to a targeted follow-up once route policy rollout order is finalized.

## What is intentionally not enforced yet

- No `[Authorize]` rollout on controllers.
- No JWT/OAuth/OIDC integration.
- No global tenant query filters.
- No hard tenant isolation enforcement at persistence boundary.
- No frontend authorization shell changes.

## Next steps P5-03/P5-04

- P5-03: authentication boundary strategy and principal propagation.
- P5-04: staged route-level authorization policies using project/building/workflow scopes.
- P5-08: security regression tests for tenant isolation and anonymous access boundaries.
