# Production SaaS Readiness Inventory

## Purpose

This document records the current security and SaaS-readiness baseline for AssistantEngineer and defines a staged migration plan for production-safe multi-user operation.

## Scope

This inventory covers:

- authentication;
- authorization;
- users;
- organizations/tenants;
- project ownership;
- tenant isolation;
- audit log;
- API key scope;
- rate limiting;
- dev-only endpoints;
- OpenAPI exposure;
- secure configuration;
- observability/security logging.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No claim that API keys are complete user authentication.
- No external identity provider integration claim.
- No certified/certification claim.

## Current state inventory

| Area | Current status | Evidence/path | Risk | Recommended P5 action |
| --- | --- | --- | --- | --- |
| Authentication | API key authentication middleware exists; fallback/default policy requires authenticated principal, but dev/testing can run with API key disabled. | `src/Backend/AssistantEngineer.Api/Configuration/ApiAuthenticationRegistration.cs`, `src/Backend/AssistantEngineer.Api/Security/ApiKey/ApiKeyAuthenticationHandler.cs`, `src/Backend/AssistantEngineer.Api/appsettings.json`, `src/Backend/AssistantEngineer.Api/appsettings.Development.json`, `tests/AssistantEngineer.Tests/Api/ApiKeyAuthenticationIntegrationTests.cs` | Environment drift can leave sensitive endpoints effectively open in non-production environments. | P5-03: define production auth mode matrix and harden environment defaults. |
| Authorization | Broad resource-level authorization is still staged; P5-09 protects development demo-data pilot, P5-10 adds read-only pilot, P5-11 adds controlled write pilot, P5-12 adds controlled execution pilot, P5-13 adds controlled report/artifact pilot protection, P5-14 adds controlled workflow read/history pilot protection, P5-15 adds a tenant-isolation integration matrix, P5-16A adds persisted Project ownership fields, P5-16B adds explicit tenant-aware query services for Projects/Buildings, P5-16C integrates those services into protected Project/Building read controllers, and P5-16D integrates workflow/scenario/job protected read paths with workflow tenant-aware query service logic. | `src/Backend/AssistantEngineer.Api/Controllers/Projects/ProjectsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Buildings/BuildingsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Calculations/EngineeringWorkflowController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Calculations/BuildingLoadCalculationsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Reports/BuildingCoolingReportsController.cs`, `docs/security/protected-endpoint-pilot-rollout.md`, `docs/security/protected-read-endpoints-rollout.md`, `docs/security/protected-write-endpoints-rollout.md`, `docs/security/protected-execution-endpoints-rollout.md`, `docs/security/protected-report-artifact-endpoints-rollout.md`, `docs/security/protected-workflow-read-history-rollout.md`, `docs/security/tenant-isolation-integration-matrix.md`, `docs/security/persistence-backed-tenant-ownership-fields.md`, `docs/security/tenant-aware-query-isolation-services.md`, `docs/security/tenant-aware-read-controller-integration.md`, `docs/security/workflow-tenant-aware-read-integration.md` | Dedicated artifact write/delete route protection, ownership backfill, metadata-incomplete workflow ownership paths, global query filters, DB row-level security, and external identity provider integration remain future protection rollouts. | P5-17/P6: harden workflow metadata ownership completeness and ownership backfill, then evaluate stricter persistence-layer enforcement planning. |
| API keys | Single shared API key model; no per-user/per-org key ownership, scope, rotation metadata, or revocation list. | `src/Backend/AssistantEngineer.Api/Security/ApiKey/ApiKeyAuthenticationSettings.cs`, `src/Backend/AssistantEngineer.Api/Security/ApiKey/ApiKeyAuthenticationHandler.cs` | Shared secret increases blast radius and prevents accountability. | P5-03: introduce principal-bound API credentials and scope model. |
| Users | No user entity/domain model in runtime persistence. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | No identity ownership model for audit, authorization, and tenancy. | P5-01: add user domain skeleton and contracts. |
| Organizations/Tenants | No organization/tenant entities or membership model. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | No first-class tenant boundary; multi-tenant readiness is incomplete. | P5-01/P5-02: add organization/membership skeleton and tenancy plan. |
| Project ownership | Projects now have nullable `OrganizationId` and `OwnerUserId` transition fields via P5-16A; P5-16B adds explicit query services, P5-16C wires protected Project/Building read controllers, and P5-16D extends read-controller integration to workflow/scenario/job protected read/history paths using workflow/project metadata where available. | `src/Backend/AssistantEngineer.Modules.Buildings/Domain/Entities/Project.cs`, `src/Backend/AssistantEngineer.Infrastructure/Persistence/Migrations/20260517102600_AddProjectTenantOwnershipFields.cs`, `docs/security/persistence-backed-tenant-ownership-fields.md`, `docs/security/tenant-aware-query-isolation-services.md`, `docs/security/tenant-aware-read-controller-integration.md`, `docs/security/workflow-tenant-aware-read-integration.md` | Legacy projects can still be unscoped until ownership backfill is completed; workflow metadata ownership coverage remains partial for some historical paths. | P5-17/P6: complete workflow metadata ownership readiness and ownership backfill planning. |
| Tenant isolation | Core Project ownership has nullable persisted tenant/user identifiers; explicit Project/Building tenant-scoped read services are integrated in P5-16C and workflow/scenario/job protected read paths are integrated in P5-16D, but global tenant query filters and DB row-level security are not enabled. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Security/TenantIsolation/ProjectTenantScopedReadService.cs`, `src/Backend/AssistantEngineer.Api/Security/TenantIsolation/BuildingTenantScopedReadService.cs`, `src/Backend/AssistantEngineer.Api/Security/TenantIsolation/WorkflowTenantScopedReadService.cs`, `docs/security/tenant-isolation-integration-matrix.md`, `docs/security/persistence-backed-tenant-ownership-fields.md`, `docs/security/tenant-aware-query-isolation-services.md`, `docs/security/tenant-aware-read-controller-integration.md`, `docs/security/workflow-tenant-aware-read-integration.md` | Cross-tenant data access risk remains in metadata-incomplete workflow query paths until ownership backfill and row-level/query-level tenant enforcement are complete. | P5-17/P6: harden workflow metadata completeness and ownership backfill; evaluate global filters only after that readiness is proven. |
| Audit log | Audit log contracts, in-memory append-only writer, metadata sanitizer, and event registry foundation are available; durable storage remains pending. | `docs/security/audit-log-foundation.md`, `docs/security/audit-event-registry.json`, `src/Backend/AssistantEngineer.Modules.Identity/Application/Abstractions/IAuditLogWriter.cs`, `src/Backend/AssistantEngineer.Modules.Identity/Application/Services/Audit/InMemoryAuditLogWriter.cs` | In-memory provider is not durable and not sufficient for production forensic retention. | P5-05B/P5-06: add durable audit provider and retention/query governance. |
| Rate limiting | Global and heavy policies exist; P5-06 adds principal-aware partition foundation (organization/user/API-key fingerprint/IP) with compatibility-disabled defaults and endpoint category model. | `docs/security/rate-limiting-foundation.md`, `docs/security/rate-limiting-policy-registry.json`, `src/Backend/AssistantEngineer.Api/Security/RateLimiting/DefaultRateLimitPartitionKeyProvider.cs`, `src/Backend/AssistantEngineer.Api/Security/RateLimiting/DefaultEndpointRateLimitCategoryResolver.cs` | Distributed/multi-node quota enforcement and production quota tuning remain future work. | P5-06 implemented foundation; continue with distributed limiter and plan-based quotas in later phases. |
| Dev-only endpoints | Development demo seeding endpoint checks `IsDevelopment()` before execution. | `src/Backend/AssistantEngineer.Api/Controllers/ReferenceData/DevelopmentDemoDataController.cs` | Future dev endpoints may miss explicit environment gating if no guard remains. | Keep guard tests for environment-gated dev endpoints. |
| Swagger/OpenAPI | OpenAPI mapping currently enabled only in Development pipeline branch. | `src/Backend/AssistantEngineer.Api/Configuration/ApiPipelineConfiguration.cs` | Uncontrolled exposure if environment configuration is incorrect. | P5-03: define production OpenAPI exposure policy and auth requirements. |
| Secrets/configuration | API key value expected via configuration/secret sources; key itself not hardcoded in tracked appsettings, but provider defaults can be permissive in dev. | `src/Backend/AssistantEngineer.Api/appsettings.json`, `src/Backend/AssistantEngineer.Api/Configuration/ApiAuthenticationRegistration.cs` | Secret handling/rotation process is not yet codified for SaaS operations. | P5-03/P5-05: codify secret management and audit requirements. |
| Observability/security logs | Structured logging policy and event taxonomy exist; no dedicated security audit trail yet. | `docs/architecture/observability-diagnostics-policy.md`, `docs/architecture/observability-diagnostic-events.json` | Security investigations may require stronger principal context and immutable events. | P5-05/P5-06: enrich principal identifiers and audit events. |
| Frontend auth state | Frontend auth-shell foundation is introduced: auth state/principal/organization models, protected content wrapper, unauthorized/forbidden UI states, and development-compatible defaults. | `docs/frontend/frontend-auth-shell-foundation.md`, `src/Frontend/src/entities/auth/model/authTypes.ts`, `src/Frontend/src/entities/auth/model/AuthContext.tsx`, `src/Frontend/src/entities/auth/ui/ProtectedContent.tsx` | Real login/provider integration and tenant-aware navigation remain future work. | P5-07 implemented as UX foundation; complete real auth integration in future staged rollout. |

## Target production model

Target SaaS model introduces:

- `User`
- `Organization` (tenant)
- `OrganizationMembership`
- `Role`
- `Permission`
- `ProjectOwnership` (project linked to owner principal and organization)
- `Building` linked to project boundary
- `Workflow/Scenario/Job` linked to project boundary
- `AuditEvent` for security and engineering actions
- API key linked to user or organization with explicit scope
- rate limiting scoped by user/org/api-key/ip

## Security boundary rules

- Controllers must not trust client-provided tenant/user identifiers without authorization checks.
- Project/building/workflow reads and writes must be scoped to authenticated principal rights.
- Development-only endpoints must be explicitly environment-gated.
- Secrets, API keys, and tokens must never be written to logs.
- Audit events must avoid full engineering payload logging.
- Public/anonymous endpoints must be explicit and documented.

## Migration principles

- No big-bang auth rewrite.
- Introduce identity domain model before route lock-down.
- Add authorization policies gradually by endpoint groups.
- Roll out route protection in controlled waves with regression tests.
- Keep local development/testing paths documented.
- Maintain tests proving no accidental public exposure.

## P5 roadmap

- P5-01 Identity domain skeleton:
  - user, organization, membership, role, permission contracts;
  - no route lock yet.
  - status: Implemented in P5-01 via `docs/security/identity-domain-skeleton.md`.
- P5-02 Project ownership and tenant scoping model:
  - ownership model for projects/buildings/workflows;
  - query filter or explicit scoping strategy.
  - status: Implemented (policy/contracts foundation only) via `docs/security/project-tenant-scoping-model.md`.
- P5-03 API authentication boundary:
  - API key/JWT strategy decision;
  - middleware and policy integration plan.
  - status: Implemented (foundation only) via `docs/security/api-authentication-boundary.md`.
- P5-04 Authorization policies:
  - project read/write;
  - building read/write;
  - workflow execute/read;
  - admin/dev endpoint access rules.
  - status: Implemented (policy + inventory foundation) via `docs/security/authorization-policy-rollout.md`.
- P5-05 Audit log:
  - security and engineering audit event persistence;
  - no full payload logging.
  - status: Implemented (foundation only) via `docs/security/audit-log-foundation.md`.
- P5-06 Rate limiting by principal:
  - user/org/API key/IP scoped policies.
  - status: Implemented (foundation + options + governance) via `docs/security/rate-limiting-foundation.md`.
- P5-07 Frontend auth shell:
  - login/session placeholder or integration;
  - protected route UX.
  - status: Implemented (foundation only) via `docs/frontend/frontend-auth-shell-foundation.md`.
- P5-08 Security regression tests:
  - anonymous access checks;
  - tenant isolation tests;
  - dev endpoint gating tests.
  - status: Implemented (governance/guardrail foundation) via `docs/security/security-regression-guardrails.md`.
- P5-09 Protected endpoint pilot rollout:
  - controlled authorization pilot on a low-risk endpoint group;
  - integration tests for 401/403/success and environment gate preservation.
  - status: Implemented (pilot only) via `docs/security/protected-endpoint-pilot-rollout.md`.
- P5-10 Protected read endpoints rollout:
  - controlled authorization pilot for selected read-only `Projects`/`Buildings` endpoints;
  - integration tests for 401/403/404/success and compatibility-default behavior.
  - status: Implemented (read-only pilot only) via `docs/security/protected-read-endpoints-rollout.md`.
- P5-11 Protected write endpoints rollout:
  - controlled authorization pilot for selected `Projects`/`Buildings` create/update/delete endpoints;
  - integration tests for 401/403/404/success and compatibility-default behavior.
  - status: Implemented (write pilot only) via `docs/security/protected-write-endpoints-rollout.md`.
- P5-12 Protected execution endpoints rollout:
  - controlled authorization pilot for selected workflow execution and calculation run endpoints;
  - integration/unit tests for 401/403/404/success-path behavior and compatibility-default mode.
  - status: Implemented (execution pilot only) via `docs/security/protected-execution-endpoints-rollout.md`.
- P5-13 Protected report/artifact endpoints rollout:
  - controlled authorization pilot for selected report/export/trace/artifact-read endpoints;
  - integration/unit tests for 401/403/404/success-path behavior and compatibility-default mode.
  - status: Implemented (report/artifact pilot only) via `docs/security/protected-report-artifact-endpoints-rollout.md`.
- P5-14 Protected workflow read/history endpoints rollout:
  - controlled authorization pilot for selected workflow state/scenario/job read endpoints;
  - integration/unit tests for 401/403/404/success-path behavior and anti-enumeration compatibility.
  - status: Implemented (workflow read/history pilot only) via `docs/security/protected-workflow-read-history-rollout.md`.
- P5-15 Tenant isolation integration matrix:
  - cross-tenant matrix for protected endpoint groups from P5-10 through P5-14;
  - gate-level matrix tests, representative endpoint smoke tests, anti-enumeration checks, and inventory coverage governance.
  - status: Implemented (test/governance hardening only) via `docs/security/tenant-isolation-integration-matrix.md`.
- P5-16A Persistence-backed tenant ownership fields:
  - nullable Project ownership fields and append-only migration;
  - resolver usage for project/building/workflow scope where metadata is available.
  - status: Implemented (persistence foundation only) via `docs/security/persistence-backed-tenant-ownership-fields.md`.
- P5-16B Tenant-aware query isolation services:
  - explicit tenant query context/policy and Project/Building tenant-scoped read services;
  - service-level tests for same-tenant, cross-tenant, anti-enumeration, and legacy-unscoped behavior.
  - status: Implemented (query isolation foundation only) via `docs/security/tenant-aware-query-isolation-services.md`.
- P5-16C Tenant-aware read controller integration:
  - protected `Projects`/`Buildings` read actions use tenant-aware query services when protected-read rollout is enabled;
  - compatibility defaults remain unchanged when protection is disabled.
  - status: Implemented (controlled controller integration) via `docs/security/tenant-aware-read-controller-integration.md`.
- P5-16D Workflow tenant-aware read integration:
  - protected workflow/scenario/job read-history actions use workflow tenant-aware query service path when workflow-read rollout is enabled;
  - compatibility defaults remain unchanged when protection is disabled.
  - status: Implemented (controlled workflow controller integration) via `docs/security/workflow-tenant-aware-read-integration.md`.

P5-01 status note:

- Identity domain skeleton is introduced.
- Runtime endpoint authorization enforcement remains a future step (P5-03/P5-04).

P5-02 status note:

- Project/building/workflow access-scope contracts and `ProjectTenantAccessPolicy` foundation are introduced.
- Route authorization and DB-level tenant enforcement remain future work (P5-03/P5-04/P5-08).

P5-03 status note:

- API authentication boundary strategy/options/principal extraction foundation are introduced.
- Broad route authorization rollout and tenant isolation enforcement remain future work (P5-04/P5-08).

P5-05 status note:

- Audit log contracts, in-memory writer, metadata sanitizer, and governance registry are introduced.
- Durable audit persistence and privileged audit query surface remain future work.

P5-06 status note:

- Rate-limiting foundation introduces partition-key and endpoint-category governance with compatibility-disabled defaults.
- Distributed rate limiting, quota billing integration, and multi-node fairness tuning remain future work.

P5-07 status note:

- Frontend auth-shell foundation is introduced with compatibility-safe defaults and no forced login.
- Real provider login/session integration and tenant-aware navigation remain future work.

P5-08 status note:

- Security regression guardrail registry and automated governance checks are introduced.
- Full route protection enforcement, tenant isolation integration tests, and distributed security controls remain future work.

P5-09 status note:

- A controlled pilot authorization check is introduced for `DevelopmentDemoDataController` behind `ApiAuthorization:EnableEndpointProtectionPilot`.
- Default compatibility remains unchanged; broad route protection rollout remains future work.

P5-10 status note:

- Read-only pilot authorization is introduced for selected `ProjectsController` and `BuildingsController` GET endpoints behind `ApiAuthorization:EnableReadEndpointProtectionPilot`.
- Default compatibility remains unchanged.
- Write/execute endpoint protection rollout remains future work.

P5-11 status note:

- Write pilot authorization is introduced for selected `ProjectsController` and `BuildingsController` create/update/delete endpoints behind `ApiAuthorization:EnableWriteEndpointProtectionPilot`.
- Default compatibility remains unchanged.
- Workflow execution, calculation run, report generation, artifact operations, and tenant-boundary enforcement remain future work.

P5-12 status note:

- Execution pilot authorization is introduced for selected workflow execution and load-calculation endpoints behind `ApiAuthorization:EnableExecutionEndpointProtectionPilot`.
- Default compatibility remains unchanged.
- Report generation, artifact operations, workflow-id ownership mapping completion, and tenant-boundary enforcement remain future work.

P5-13 status note:

- Report/artifact pilot authorization is introduced for selected report/export/trace/artifact-read endpoints behind `ApiAuthorization:EnableReportArtifactEndpointProtectionPilot`.
- Default compatibility remains unchanged.
- Workflow read/history endpoint rollout and complete tenant-boundary enforcement remain future work.

P5-14 status note:

- Workflow read/history pilot authorization is introduced for selected workflow state/scenario/job read endpoints behind `ApiAuthorization:EnableWorkflowReadEndpointProtectionPilot`.
- Default compatibility remains unchanged.
- Dedicated artifact write/delete route protection and complete tenant-boundary enforcement remain future work.

P5-15 status note:

- Tenant isolation integration matrix documentation, JSON registry, schema, gate-level matrix tests, representative API smoke tests, anti-enumeration tests, and inventory coverage checks are introduced.
- Default compatibility remains unchanged.
- DB row-level security, global query filters, dedicated artifact write/delete endpoint protection, ownership backfill, and external identity provider integration remain future work.

P5-16A status note:

- Nullable `Project.OrganizationId` and `Project.OwnerUserId` fields are introduced with indexes and resolver usage.
- Default compatibility remains unchanged, and no route/API/DTO behavior is changed.
- Global query filters, database row-level security, ownership backfill, dedicated artifact write/delete endpoints, and external identity provider integration remain future work.

P5-16B status note:

- Explicit `TenantQueryContext`, `TenantQueryIsolationPolicy`, `IProjectTenantScopedReadService`, and `IBuildingTenantScopedReadService` are introduced.
- Project/Building tenant-scoped read tests prove cross-tenant resources are excluded and legacy unscoped resources remain explicit transition behavior.
- Default compatibility remains unchanged, controllers are not rewired, and no route/API/DTO behavior is changed.
- Global query filters, database row-level security, controller integration, ownership backfill, workflow tenant-scoped read service integration, and external identity provider integration remain future work.

P5-16C status note:

- Protected `Projects`/`Buildings` read controllers now consume tenant-aware query services when P5-10 protected-read rollout options are enabled.
- Default compatibility remains unchanged when protection is disabled.
- Workflow tenant-scoped read controller integration for read/history is delivered in P5-16D.
- Global query filters, database row-level security, ownership backfill, and external identity provider integration remain future work.

P5-16D status note:

- Protected workflow read/history controllers now consume workflow tenant-aware query service paths when workflow-read rollout options are enabled.
- Default compatibility remains unchanged when protection is disabled.
- Metadata-incomplete workflow ownership paths remain staged and compatibility-option controlled.
- Global query filters, database row-level security, ownership backfill, and external identity provider integration remain future work.
