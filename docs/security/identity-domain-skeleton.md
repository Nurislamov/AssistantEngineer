# Identity Domain Skeleton

## Purpose

This document defines the P5-01 identity domain foundation for future production SaaS authorization boundaries.

## Scope

P5-01 introduces:

- identity domain entities;
- role and permission model;
- identity value objects;
- repository abstractions;
- permission policy.

This step intentionally avoids runtime route authorization changes.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.

## Domain model

Identity module includes:

- `User`
- `Organization`
- `OrganizationMembership`

Core invariants include required identity fields, active/inactive transitions, membership lifecycle controls, and normalized identity value handling.

`User.ExternalSubjectId` is reserved as the future mapping point for JWT/OIDC external subject identifiers.

## Role model

Organization role set:

- `Owner`
- `Admin`
- `Engineer`
- `Viewer`

## Permission model

Permission set:

- `ProjectsRead`
- `ProjectsWrite`
- `BuildingsRead`
- `BuildingsWrite`
- `WorkflowsRead`
- `WorkflowsExecute`
- `ReportsRead`
- `ReportsWrite`
- `AdministrationManage`

`OrganizationPermissionPolicy` maps roles to permission sets.

## Repository abstractions

Identity application layer defines repository abstractions only:

- `IIdentityUserRepository`
- `IOrganizationRepository`
- `IOrganizationMembershipRepository`

No production EF repository implementation is introduced in P5-01.

## What is intentionally not implemented yet

- No endpoint authorization lock-down.
- No JWT/OAuth/OIDC implementation.
- No external identity provider integration.
- No tenant isolation enforcement.
- No users/organizations controllers.
- No identity database migration in this step.

## Next steps P5-02/P5-03

- P5-02: project ownership and tenant scoping model with explicit data boundary mapping.
- P5-03: authentication boundary strategy (scoped API keys and/or token model), then staged policy wiring.
