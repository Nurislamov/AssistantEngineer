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
| Authorization | Broad resource-level authorization is still staged; P5-09 protects development demo-data pilot, P5-10 adds read-only pilot, P5-11 adds controlled write pilot, P5-12 adds controlled execution pilot, and P5-13 adds controlled report/artifact pilot protection. | `src/Backend/AssistantEngineer.Api/Controllers/Projects/ProjectsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Buildings/BuildingsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Calculations/EngineeringWorkflowController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Calculations/BuildingLoadCalculationsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Reports/BuildingCoolingReportsController.cs`, `docs/security/protected-endpoint-pilot-rollout.md`, `docs/security/protected-read-endpoints-rollout.md`, `docs/security/protected-write-endpoints-rollout.md`, `docs/security/protected-execution-endpoints-rollout.md`, `docs/security/protected-report-artifact-endpoints-rollout.md` | Workflow read/history endpoints and full tenant-isolation enforcement remain future protection rollouts. | P5-13 implemented as report/artifact pilot for selected endpoint groups; continue staged rollout for workflow history and deeper tenant isolation tests. |
| API keys | Single shared API key model; no per-user/per-org key ownership, scope, rotation metadata, or revocation list. | `src/Backend/AssistantEngineer.Api/Security/ApiKey/ApiKeyAuthenticationSettings.cs`, `src/Backend/AssistantEngineer.Api/Security/ApiKey/ApiKeyAuthenticationHandler.cs` | Shared secret increases blast radius and prevents accountability. | P5-03: introduce principal-bound API credentials and scope model. |
| Users | No user entity/domain model in runtime persistence. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | No identity ownership model for audit, authorization, and tenancy. | P5-01: add user domain skeleton and contracts. |
| Organizations/Tenants | No organization/tenant entities or membership model. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | No first-class tenant boundary; multi-tenant readiness is incomplete. | P5-01/P5-02: add organization/membership skeleton and tenancy plan. |
| Project ownership | Projects exist, but no principal ownership fields or access-control mapping. | `src/Backend/AssistantEngineer.Modules.Buildings/Domain/Entities/Project.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Projects/ProjectsController.cs` | Project data can be queried by id without ownership enforcement. | P5-02/P5-04: add project ownership model and policy checks. |
| Tenant isolation | No tenant id on core entities and no tenant query filters. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | Cross-tenant data access risk once multi-tenant usage begins. | P5-02: define tenant boundary and scoping strategy before auth rollout. |
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
- Workflow execution, calculation run, report generation, artifact operations, and full tenant isolation remain future work.

P5-12 status note:

- Execution pilot authorization is introduced for selected workflow execution and load-calculation endpoints behind `ApiAuthorization:EnableExecutionEndpointProtectionPilot`.
- Default compatibility remains unchanged.
- Report generation, artifact operations, workflow-id ownership mapping completion, and full tenant isolation remain future work.

P5-13 status note:

- Report/artifact pilot authorization is introduced for selected report/export/trace/artifact-read endpoints behind `ApiAuthorization:EnableReportArtifactEndpointProtectionPilot`.
- Default compatibility remains unchanged.
- Workflow read/history endpoint rollout and complete tenant-boundary enforcement remain future work.
