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

Canonical release-boundary reference: `docs/security/security-release-boundary.md`.

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
| Authorization | Broad resource-level authorization is still staged; P5-09 protects development demo-data pilot, P5-10 adds read-only pilot, P5-11 adds controlled write pilot, P5-12 adds controlled execution pilot, P5-13 adds controlled report/artifact pilot protection, P5-14 adds controlled workflow read/history pilot protection, P5-15 adds a tenant-isolation integration matrix, P5-16A adds persisted Project ownership fields, P5-16B adds explicit tenant-aware query services for Projects/Buildings, P5-16C integrates those services into protected Project/Building read controllers, P5-16D integrates workflow/scenario/job protected read paths with workflow tenant-aware query service logic, and P5-17 hardens workflow ownership metadata coverage inventory/resolver behavior. | `src/Backend/AssistantEngineer.Api/Controllers/Projects/ProjectsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Buildings/BuildingsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Calculations/EngineeringWorkflowController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Calculations/BuildingLoadCalculationsController.cs`, `src/Backend/AssistantEngineer.Api/Controllers/Reports/BuildingCoolingReportsController.cs`, `docs/security/protected-endpoint-pilot-rollout.md`, `docs/security/protected-read-endpoints-rollout.md`, `docs/security/protected-write-endpoints-rollout.md`, `docs/security/protected-execution-endpoints-rollout.md`, `docs/security/protected-report-artifact-endpoints-rollout.md`, `docs/security/protected-workflow-read-history-rollout.md`, `docs/security/tenant-isolation-integration-matrix.md`, `docs/security/persistence-backed-tenant-ownership-fields.md`, `docs/security/tenant-aware-query-isolation-services.md`, `docs/security/tenant-aware-read-controller-integration.md`, `docs/security/workflow-tenant-aware-read-integration.md`, `docs/security/workflow-ownership-metadata-coverage.md` | Dedicated artifact write/delete route protection, ownership backfill, metadata-incomplete workflow ownership paths, global query filters, DB row-level security, and external identity provider integration remain future protection rollouts. | P6: execute ownership backfill, then evaluate stricter persistence-layer enforcement planning. |
| API keys | Single shared API key model; no per-user/per-org key ownership, scope, rotation metadata, or revocation list. | `src/Backend/AssistantEngineer.Api/Security/ApiKey/ApiKeyAuthenticationSettings.cs`, `src/Backend/AssistantEngineer.Api/Security/ApiKey/ApiKeyAuthenticationHandler.cs` | Shared secret increases blast radius and prevents accountability. | P5-03: introduce principal-bound API credentials and scope model. |
| Users | No user entity/domain model in runtime persistence. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | No identity ownership model for audit, authorization, and tenancy. | P5-01: add user domain skeleton and contracts. |
| Organizations/Tenants | No organization/tenant entities or membership model. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceDbContext.cs` | No first-class tenant boundary; multi-tenant readiness is incomplete. | P5-01/P5-02: add organization/membership skeleton and tenancy plan. |
| Project ownership | Projects now have nullable `OrganizationId` and `OwnerUserId` transition fields via P5-16A; P5-16B adds explicit query services, P5-16C wires protected Project/Building read controllers, P5-16D extends read-controller integration to workflow/scenario/job protected read/history paths, and P5-17 adds explicit workflow ownership metadata coverage inventory. | `src/Backend/AssistantEngineer.Modules.Buildings/Domain/Entities/Project.cs`, `src/Backend/AssistantEngineer.Infrastructure/Persistence/Migrations/20260517102600_AddProjectTenantOwnershipFields.cs`, `docs/security/persistence-backed-tenant-ownership-fields.md`, `docs/security/tenant-aware-query-isolation-services.md`, `docs/security/tenant-aware-read-controller-integration.md`, `docs/security/workflow-tenant-aware-read-integration.md`, `docs/security/workflow-ownership-metadata-coverage.md` | Legacy projects can still be unscoped until ownership backfill is completed; workflow metadata ownership coverage remains partial for some historical paths. | P6: execute ownership backfill and close remaining metadata partial paths. |
| Tenant isolation | Core Project ownership has nullable persisted tenant/user identifiers; explicit Project/Building tenant-scoped read services are integrated in P5-16C, workflow/scenario/job protected read paths are integrated in P5-16D, and workflow ownership metadata coverage is inventoried/hardened in P5-17, but global tenant query filters and DB row-level security are not enabled. | `src/Backend/AssistantEngineer.Infrastructure/Persistence/AppDbContext.cs`, `src/Backend/AssistantEngineer.Api/Security/TenantIsolation/ProjectTenantScopedReadService.cs`, `src/Backend/AssistantEngineer.Api/Security/TenantIsolation/BuildingTenantScopedReadService.cs`, `src/Backend/AssistantEngineer.Api/Security/TenantIsolation/WorkflowTenantScopedReadService.cs`, `docs/security/tenant-isolation-integration-matrix.md`, `docs/security/persistence-backed-tenant-ownership-fields.md`, `docs/security/tenant-aware-query-isolation-services.md`, `docs/security/tenant-aware-read-controller-integration.md`, `docs/security/workflow-tenant-aware-read-integration.md`, `docs/security/workflow-ownership-metadata-coverage.md` | Cross-tenant data access risk remains in metadata-incomplete workflow query paths until ownership backfill and row-level/query-level tenant enforcement are complete. | P6: ownership backfill and deeper persistence-layer tenant enforcement planning. |
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
- P5-17 Workflow ownership metadata coverage:
  - workflow/scenario/job ownership metadata inventory (markdown + JSON + schema);
  - resolver and tenant-aware read strict-mode hardening for unresolved ownership metadata paths.
  - status: Implemented (metadata coverage hardening) via `docs/security/workflow-ownership-metadata-coverage.md`.
- P6-00 Ownership backfill strategy/evidence model:
  - strategy-only dry-run-first plan, safety policy, governance gates, and evidence artifacts;
  - no backfill execution in this step.
  - status: StrategyOnly via `docs/security/ownership-backfill-strategy.md` and `docs/security/ownership-backfill-evidence-model.md`.
- P6-01 Ownership backfill dry-run tool skeleton:
  - dry-run-only CLI foundation with no-data scanner and evidence writers for summary/unresolved/previous-values artifacts;
  - apply mode remains unsupported and no backfill execution is performed.
  - status: Implemented via `docs/security/ownership-backfill-dry-run-tool.md`.
- P6-02 Ownership backfill database dry-run scanner:
  - read-only SQLite/PostgreSQL scanner computes ownership coverage metrics and unresolved/ambiguous evidence;
  - apply mode remains unsupported and no backfill execution is performed.
  - status: Implemented via `docs/security/ownership-backfill-database-dry-run-scanner.md`.
- P6-03 Backfill evidence validation gates:
  - `validate-evidence` command validates dry-run evidence structure, required metrics, unresolved thresholds, and ambiguous ownership policy;
  - gate outputs are run-scoped JSON/Markdown artifacts, and apply mode remains unsupported.
  - status: Implemented via `docs/security/ownership-backfill-evidence-validation-gates.md`.
- P6-04 Apply mode design (disabled):
  - apply-mode preconditions, confirmation phrase, and evidence contracts are documented in design artifacts;
  - apply command path remains disabled and non-zero with no write execution.
  - status: DesignOnly via `docs/security/ownership-backfill-apply-mode-design.md`.
- P6-05 Apply plan generator (no-write):
  - `plan-apply` generates deterministic apply-plan artifacts from passed dry-run evidence + passed gate result;
  - apply mode remains disabled and no ownership metadata writes are performed.
  - status: Implemented via `docs/security/ownership-backfill-apply-plan-generator.md`.
- P6-06 Apply plan sign-off gate (no-write):
  - `signoff-plan` validates deterministic plan hash, reviewer/ticket metadata, and writes sign-off governance artifacts;
  - apply mode remains disabled and no ownership metadata writes are performed.
  - status: Implemented via `docs/security/ownership-backfill-plan-signoff-gate.md`.
- P6-07 Test-only apply executor rehearsal:
  - test-only executor interfaces and in-memory/temp-store simulation validate batch/idempotency/conflict and previous-values behavior;
  - production CLI apply remains disabled and no production DB writes are enabled.
  - status: Implemented via `docs/security/ownership-backfill-test-only-apply-rehearsal.md`.
- P6-08 Real apply enablement readiness checklist (no-write):
  - `validate-apply-readiness` validates dry-run/gate/plan/sign-off/previous-values consistency, deterministic `ApplyInputHash`, sign-off TTL, and rollback-readiness evidence;
  - apply mode remains disabled and no ownership metadata writes are performed.
  - status: Implemented via `docs/security/ownership-backfill-apply-enablement-readiness.md`.
- P6-09 Production apply enablement proposal (no-write):
  - formal staging/production approval policy, go/no-go criteria, backup/rollback readiness requirements, and change-management template are documented;
  - apply mode remains disabled and no ownership metadata writes are performed.
  - status: GovernanceOnly via `docs/security/ownership-backfill-production-apply-enablement-proposal.md`.
- P6-10 Staging apply enablement design (no-write):
  - staging-specific runbook/checklist governance, operator/environment policy, backup/rollback rehearsal requirements, and promotion criteria are documented;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: DesignOnly via `docs/security/ownership-backfill-staging-apply-runbook.md`.
- P6-11 Staging apply executor design (no-write):
  - staging-only executor contract, preflight checks, environment/schema/backup/hash-chain gates, and post-run evidence contract are documented;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: DesignOnly via `docs/security/ownership-backfill-staging-apply-executor-design.md`.
- P6-12 Staging post-run evidence contract and acceptance validator (no-write):
  - deterministic post-run evidence contract, `StagingRunHash`, and `validate-staging-acceptance` acceptance/rejection validator are documented;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: Implemented via `docs/security/ownership-backfill-staging-post-run-evidence.md`.
- P6-13 Production promotion readiness proposal (no-write):
  - formal production promotion readiness contract validates accepted staging evidence, separate production evidence chain, hash separation, and production change-request binding;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: GovernanceOnly via `docs/security/ownership-backfill-production-promotion-readiness.md`.
- P6-14 Manual write-path enablement decision framework (no-write):
  - human-only decision packet/checklist/template governance binds approvals to `ProductionPromotionHash` and `ApplyInputHash` with TTL checks;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: GovernanceOnly via `docs/security/ownership-backfill-manual-write-path-enablement-decision.md`.
- P6-15 Apply enablement architecture review (no-write):
  - architecture invariants/checklist define no-wiring/no-secrets/no-destructive-sql boundaries and mandatory disabled-apply governance before any future code enablement;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: GovernanceOnly via `docs/security/ownership-backfill-apply-enablement-architecture-review.md`.
- P7-00 Post-P6 governance audit and release boundary review (audit-only):
  - post-P6 audit/index validates claims consistency, disabled apply boundary, source-level no-wiring posture, docs/schema consistency, and generated artifact policy;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: AuditOnly via `docs/security/post-p6-governance-audit.md`.
- P7-01 Security governance docs deduplication and index normalization:
  - canonical `security-release-boundary` and normalized `security-governance-index`/status vocabulary reduce status-text drift;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: GovernanceOnly via `docs/security/security-release-boundary.md`.
- P7-02 Governance test consolidation:
  - shared governance test helpers reduce copy-paste and keep no-write/disabled-boundary coverage centralized;
  - staging and production apply remain disabled and no ownership metadata writes are performed.
  - status: GovernanceOnly via `docs/security/governance-test-consolidation-report.md`.

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

P5-17 status note:

- Workflow/scenario/job ownership metadata coverage is explicitly inventoried and machine-readable.
- Resolver logic now treats invalid or missing ownership identifiers as unresolved scope and uses scenario linkage metadata first for job scope.
- No migration and no ownership backfill are performed in this step.
- Global query filters, database row-level security, ownership backfill, and external identity provider integration remain future work.

P6-00 status note:

- Ownership backfill strategy and evidence model are documented with machine-readable descriptors.
- Dry-run metrics, safety checks, and governance gates are defined.
- No data backfill is executed in this step.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-01 status note:

- Ownership backfill dry-run tool skeleton is implemented with dry-run-only behavior.
- No-data mode is the only active scanner mode in this step.
- Apply mode is explicitly rejected and no persistence writes are executed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-02 status note:

- Database dry-run scanner is implemented for explicit SQLite/PostgreSQL connections.
- Scanner remains read-only (`AsNoTracking`, no `SaveChanges`, no destructive SQL).
- Unresolved and ambiguous ownership evidence is emitted through existing dry-run evidence artifacts.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-03 status note:

- Evidence validation gates are implemented with pass/fail results and threshold policy defaults.
- Ambiguous ownership fails by default and required record-type metrics are enforced.
- Apply mode remains explicitly disabled and no persistence writes are executed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-04 status note:

- Apply-mode design contracts and precondition validation models are introduced.
- Apply command remains explicitly disabled and always non-zero in this stage.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-05 status note:

- Plan-only command generates deterministic plan artifacts and `PlanHash` from passed evidence.
- Apply command remains explicitly disabled and always non-zero in this stage.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-06 status note:

- Plan sign-off command validates `PlanHash` and reviewer/ticket metadata and produces sign-off artifacts only.
- Apply command remains explicitly disabled and always non-zero in this stage.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-07 status note:

- Test-only apply executor rehearsal validates batch execution, conflict handling, idempotency, and previous-values capture in controlled test stores.
- Production CLI apply remains explicitly disabled and always non-zero in this stage.
- No production DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-08 status note:

- Apply-enablement readiness gate validates deterministic hash chain (`PlanHash` + sign-off hash + `ApplyInputHash`) and artifact consistency before any future enablement stage.
- Sign-off TTL and previous-values completeness checks are enforced at readiness validation time.
- Apply command remains explicitly disabled and always non-zero in this stage.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-09 status note:

- Production/staging apply enablement proposal is documented with mandatory approval policy and change-management fields.
- `ApplyInputHash` is required as a change identifier in the proposed release process.
- Backup/restore and rollback readiness are explicit go/no-go gates in the proposal.
- Apply command remains explicitly disabled and always non-zero in this stage.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-10 status note:

- Staging apply runbook and staging acceptance checklist are documented as governance-only artifacts.
- `ApplyInputHash` is mandatory in staging evidence chain and acceptance records.
- Staging operator/environment separation, backup/restore readiness, rollback rehearsal, and promotion rules are explicit.
- Apply command remains explicitly disabled and always non-zero in this stage.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-11 status note:

- Staging executor contract and preflight validator design are introduced with staging-only environment guard requirements.
- `validate-staging-preflight` can validate no-write preflight inputs, but no staging apply execution is enabled.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-12 status note:

- Staging post-run evidence contract defines required artifact chain and deterministic `StagingRunHash`.
- `validate-staging-acceptance` validates acceptance/rejection criteria in no-write mode only.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-13 status note:

- Production promotion readiness proposal defines accepted staging evidence requirements and production-specific evidence-chain separation.
- `validate-production-promotion` validates production promotion decision artifacts and deterministic `ProductionPromotionHash` in no-write mode only.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-14 status note:

- Manual write-path enablement decision framework defines human-only approval packet, TTL/expiry checks, and go/no-go review criteria bound to `ProductionPromotionHash` and `ApplyInputHash`.
- Manual decision artifacts are governance-only and cannot enable runtime apply behavior.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P6-15 status note:

- Apply-enablement architecture review defines mandatory invariants (`ApplyDisabledInvariant`, `EnvironmentHardDenyInvariant`, hash-chain, rollback completeness, no-secrets/no-payload, and no production wiring) before any future code enablement stage.
- Architecture review checklist artifacts are governance-only and cannot enable runtime apply behavior.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P7-00 status note:

- Post-P6 governance audit reviews P5/P6 security/tenant/backfill release boundary and consolidates findings/backlog without changing runtime behavior.
- Security governance index centralizes P5/P6/P7 references and non-claim boundaries.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P7-01 status note:

- `security-release-boundary.md` is now the canonical source for enabled/disabled release capability claims.
- Security-governance index and status vocabulary are normalized to reduce repeated status text drift.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.

P7-02 status note:

- Governance test helper layer (`tests/AssistantEngineer.Tests/Architecture/Governance/`) consolidates repeated docs/json/claim/source-scan assertions.
- P7 governance tests and selected high-duplication P6 tests were refactored to shared helper assertions.
- Staging and production apply remain explicitly disabled and always non-zero for real apply path.
- No DB write path is enabled and no ownership backfill execution is performed.
- Global query filters, database row-level security, ownership backfill execution, and external identity provider integration remain future work.
## P7-03 status note

- P7-03 Ownership backfill CLI UX cleanup is implemented as tooling/governance hardening only.
- Apply remains disabled; no runtime write-path behavior change is claimed.

## P7-04 status note

- P7-04 release-ready observability/performance audit is implemented as audit/tooling diagnostics hardening only.
- Runtime behavior and write-path/apply boundaries remain unchanged.

## P7-05 status note

- P7-05 CI/GitHub checks visibility is implemented as audit/governance hardening only.
- CI visibility contract and runbook are documented; workflow safety checks are test-governed.
- Runtime behavior and write-path/apply boundaries remain unchanged.

P7-05 reference:

- `docs/security/ci-github-checks-visibility-audit.md`
- `docs/security/ci-github-checks-visibility-runbook.md`

## P7-06 status note

- P7-06 route inventory and claims consistency automation is implemented as governance hardening only.
- Route classification model and inventory consistency checks are test-governed; runtime routes and controller behavior are unchanged.
- Runtime behavior and write-path/apply boundaries remain unchanged.

P7-06 reference:

- `docs/security/route-inventory-claims-consistency-audit.md`
- `docs/security/api-endpoint-classification-model.md`

## P7-07 status note

- P7-07 security docs map and decision record consolidation is implemented as governance/documentation hardening only.
- Canonical docs map and accepted ADR boundary are documented without enabling runtime write paths.
- Runtime behavior and write-path/apply boundaries remain unchanged.

P7-07 reference:

- `docs/security/security-docs-map.md`
- `docs/adr/ADR-0001-security-governance-boundary.md`
- `docs/adr/adr-index.md`

## P7-08 status note

- P7-08 post-P6 architecture decision record consolidation is implemented as governance hardening only.
- Security architecture decision matrix and future ADR backlog are added to centralize accepted/deferred/rejected boundary decisions.
- Runtime behavior and write-path/apply boundaries remain unchanged.

P7-08 reference:

- `docs/adr/security-architecture-decision-matrix.md`
- `docs/adr/future-security-adr-backlog.md`

## P8-00 status note

- P8-00 engineering/domain architecture audit is implemented as audit-only architecture governance hardening.
- Findings and backlog are documented without runtime/API/write-path/calculation-physics behavior changes.

P8-00 reference:

- `docs/architecture/engineering-domain-architecture-audit.md`
- `docs/architecture/assistantengineer-architecture-map.md`
- `docs/architecture/legacy-and-dead-code-inventory.md`
- `docs/architecture/scripts-tools-inventory.md`

## P8-01 status note

- P8-01 EngineeringWorkflow boundary and naming hardening is implemented as refactor/governance hardening only.
- EngineeringWorkflow module application namespaces were normalized to `AssistantEngineer.Modules.EngineeringWorkflow.Application.*`.
- Runtime behavior, public API routes/DTO shapes, write-path/apply boundary, and calculation physics remain unchanged.

P8-01 reference:

- `docs/architecture/engineeringworkflow-boundary-hardening.md`

## P8-02 status note

- P8-02 module-boundary test expansion is implemented as architecture-test/governance hardening only.
- Shared module-boundary matrix and EngineeringWorkflow-specific boundary checks are added without runtime/API/write-path/calculation-physics behavior changes.

P8-02 reference:

- `docs/architecture/module-boundary-matrix.md`
- `docs/architecture/module-boundary-test-expansion.md`

## P8-03 status note

- P8-03 authorization/workflow hotspot decomposition remains design-led for extraction stages.
- Hotspot inventory and staged decomposition plan are documented with explicit no-behavior-change compatibility requirements.
- Runtime behavior, protected endpoint semantics, public routes/DTOs, write-path/apply boundary, and calculation physics remain unchanged.

P8-03 reference:

- `docs/architecture/authorization-workflow-hotspot-inventory.md`
- `docs/architecture/authorization-workflow-decomposition-design.md`

## P8-03A status note

- P8-03A protected endpoint authorization gate characterization tests are implemented as test-only behavior freeze.
- Decision/status matrix, option-gate behavior, tenant-mismatch anti-enumeration outcomes, and deny-log redaction expectations are documented and test-governed.
- Runtime behavior, authorization semantics, public routes/DTOs, write-path/apply boundary, and calculation physics remain unchanged.

P8-03A reference:

- `docs/architecture/protected-endpoint-authorization-gate-characterization.md`

## P8-03C status note

- P8-03C protected-endpoint authorization gate permission/scope extraction is implemented as internal refactor-only decomposition.
- Gate facade contract is unchanged and characterization tests continue to lock decision/status semantics.
- Runtime behavior, authorization semantics, public routes/DTOs, write-path/apply boundary, and calculation physics remain unchanged.

P8-03C reference:

- `docs/architecture/protected-endpoint-authorization-gate-decomposition.md`

## P8-03D status note

- P8-03D workflow controller shell characterization tests are implemented as test-only behavior freeze for route/action/status/response compatibility.
- EngineeringWorkflowController decomposition moved to P8-03E/P8-03F staged implementation.
- Workflow behavior, authorization semantics, public routes/DTOs, write-path/apply boundary, and calculation physics remain unchanged.

P8-03D reference:

- `docs/architecture/workflow-controller-shell-characterization.md`

## P8-03E status note

- P8-03E workflow orchestration helper migration is implemented as internal refactor-only boundary hardening.
- `EngineeringWorkflowDiagnosticsService`, `EngineeringWorkflowStateBuilder`, and `EngineeringWorkflowSubmissionService` are migrated to EngineeringWorkflow module application paths while API routes/actions/DTO contracts remain unchanged.
- Workflow behavior, authorization semantics, write-path/apply boundary, and calculation physics remain unchanged.

P8-03E reference:

- `docs/architecture/workflow-orchestration-helper-migration.md`

## P8-03F status note

- P8-03F workflow controller shell reduction is implemented as internal refactor-only adapter cleanup.
- Main controller-shell validate/prepare orchestration is extracted to API-local `EngineeringWorkflowControllerActionService` while route/action signatures and status/response behavior remain unchanged.
- Workflow behavior, authorization semantics, write-path/apply boundary, and calculation physics remain unchanged.

P8-03F reference:

- `docs/architecture/workflow-controller-shell-reduction.md`

## P8-04 status note

- P8-04 OwnershipBackfill CLI parser simplification is implemented as tooling-only refactor.
- Parser command dispatch now uses descriptor catalog/argument-reader collaborators while command names, argument names, help semantics, exit-code meanings, redaction guarantees, and apply-disabled boundary remain unchanged.
- Runtime API behavior, workflow/calculation behavior, and DB write-path boundary remain unchanged.

P8-04 reference:

- `docs/architecture/ownershipbackfill-cli-parser-simplification.md`

## P8-05 status note

- P8-05 route inventory deferred classification closure is implemented as governance-only refinement.
- Unknown route classifications were reduced and ignore-list coverage was tightened with explicit replacement inventory entries.
- Runtime/API route behavior, authorization semantics, write-path/apply boundary, and calculation physics remain unchanged.

P8-05 reference:

- `docs/architecture/route-inventory-classification-closure.md`

## P8-06 status note

- P8-06 scripts/tools rationalization is implemented as governance/tooling boundary refinement only.
- Wrapper/tool/workflow criticality inventory is normalized to reduce ambiguity in future migration/deprecation decisions.
- Release-ready semantics, CI gate behavior, runtime/API behavior, write-path/apply boundary, and calculation physics remain unchanged.

P8-06 reference:

- `docs/architecture/scripts-tools-rationalization.md`

## P8-07 status note

- P8-07 terminology and claims-surface cleanup is implemented as governance-only wording normalization.
- Canonical allowed/forbidden claims vocabulary is now explicit and linked from security/architecture indexes and release-boundary docs.
- Runtime/API behavior, authorization semantics, workflow/calculation behavior, and write-path/apply boundary remain unchanged.

P8-07 reference:

- `docs/architecture/terminology-and-claims-vocabulary.md`
- `docs/architecture/terminology-claims-surface-cleanup.md`

## P8-08 status note

- P8-08 governance test brittleness reduction phase 2 is implemented as test/governance hardening only.
- Selected wording-coupled governance assertions are replaced with shared semantic JSON/non-claim checks while critical behavior-level guardrails remain strict.
- Runtime/API behavior, authorization semantics, workflow/calculation behavior, and write-path/apply boundary remain unchanged.

P8-08 reference:

- `docs/architecture/governance-test-brittleness-reduction.md`

## P8-09 status note

- P8-09 final closure audit consolidates P8 findings resolution, deferred backlog, and preserved boundary guarantees.
- Runtime/API behavior, authorization semantics, workflow/calculation behavior, and write-path/apply boundary remain unchanged.

P8-09 reference:

- `docs/architecture/p8-engineering-domain-hardening-closure.md`

## P9-00 status note

- P9-00 engineering calculation validation roadmap refresh is implemented as audit-only governance planning.
- Validation roadmap, evidence inventory, and claims policy are documented to improve calculation-confidence planning without changing runtime or physics behavior.
- Runtime/API behavior, authorization semantics, workflow behavior, calculation formulas, and write-path/apply boundary remain unchanged.

P9-00 reference:

- `docs/validation/engineering-calculation-validation-roadmap.md`
- `docs/validation/validation-evidence-inventory.md`
- `docs/validation/validation-claims-policy.md`

## P9-01 status note

- P9-01 ISO52016 decomposition review is implemented as audit-only architecture/validation planning.
- ISO52016 component map and hotspot inventory are documented for staged characterization-first decomposition, without code-level formula or expected-value changes.
- Runtime/API behavior, authorization semantics, workflow behavior, calculation formulas, expected numerical values, and write-path/apply boundary remain unchanged.

P9-01 reference:

- `docs/validation/iso52016-decomposition-review.md`
- `docs/validation/iso52016-component-map.md`

## P9-01A status note

- P9-01A ISO52016 behavior characterization inventory is implemented as test-only seam behavior freeze prior to decomposition stages.
- Component-level coverage classification, retained-gap inventory, and focused deterministic characterization tests are added without formula or expected-value fixture edits.
- Runtime/API behavior, authorization semantics, workflow behavior, calculation formulas, expected numerical values, and write-path/apply boundary remain unchanged.

P9-01A reference:

- `docs/validation/iso52016-behavior-characterization-inventory.md`

## P9-01B status note

- P9-01B ISO52016 matrix/solver seam extraction design is implemented as design-only decomposition planning.
- Proposed seams, invariants, extraction-sequence stages, and seam risk register are documented without formula, expected-value, fixture, runtime, or API behavior changes.
- Runtime/API behavior, authorization semantics, workflow behavior, calculation formulas, expected numerical values, and write-path/apply boundary remain unchanged.

P9-01B reference:

- `docs/validation/iso52016-matrix-solver-seam-design.md`
- `docs/validation/iso52016-matrix-solver-seam-risk-register.md`

## P9-01B1 status note

- P9-01B1 ISO52016 matrix/solver characterization hardening is implemented as test-only pre-extraction hardening.
- Focused matrix/vector/kernel/coupling/diagnostics characterization tests are strengthened without formula, expected-value, fixture, runtime, or API behavior changes.
- Runtime/API behavior, authorization semantics, workflow behavior, calculation formulas, expected numerical values, and write-path/apply boundary remain unchanged.

P9-01B1 reference:

- `docs/validation/iso52016-matrix-solver-characterization-hardening.md`

## P9-03 status note

- P9-03 validation fixture provenance cleanup is implemented as governance/docs metadata normalization.
- Provenance model and provenance inventory now separate achieved evidence from planned placeholders and enforce explicit claim boundaries.
- Runtime/API behavior, authorization semantics, workflow behavior, calculation formulas, expected numerical values, and write-path/apply boundary remain unchanged.

P9-03 reference:

- `docs/validation/validation-fixture-provenance-model.md`
- `docs/validation/validation-fixture-provenance-inventory.md`
