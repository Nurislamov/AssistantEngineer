# Security Regression Guardrails

## Purpose

This document defines security regression guardrails for staged SaaS hardening so that security posture does not drift between roadmap stages.

## Scope

Guardrails cover:

- route protection inventory;
- development-only endpoint gating;
- authentication boundary defaults;
- authorization rollout governance;
- audit log sanitization safety;
- rate limiting safety defaults;
- secret logging/source leak prevention;
- secure configuration checks;
- false security/compliance claim prevention;
- frontend auth shell and frontend secret safety.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.

## Guardrail categories

- `SEC-GUARD-ROUTE-INVENTORY`
- `SEC-GUARD-DEV-ENDPOINT`
- `SEC-GUARD-SECRET-LOGGING`
- `SEC-GUARD-AUTH-DEFAULTS`
- `SEC-GUARD-RATE-LIMIT-DEFAULTS`
- `SEC-GUARD-AUDIT-SANITIZATION`
- `SEC-GUARD-INMEMORY-PRODUCTION`
- `SEC-GUARD-FALSE-CLAIMS`
- `SEC-GUARD-FRONTEND-SECRETS`
- `SEC-GUARD-TENANT-ISOLATION-MATRIX`
- `SEC-GUARD-PERSISTED-TENANT-OWNERSHIP`
- `SEC-GUARD-TENANT-QUERY-ISOLATION`
- `SEC-GUARD-TENANT-READ-CONTROLLER-INTEGRATION`
- `SEC-GUARD-WORKFLOW-TENANT-READ-INTEGRATION`
- `SEC-GUARD-WORKFLOW-OWNERSHIP-METADATA-COVERAGE`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STRATEGY-EVIDENCE`
- `SEC-GUARD-OWNERSHIP-BACKFILL-DRY-RUN-TOOL`
- `SEC-GUARD-OWNERSHIP-BACKFILL-DATABASE-DRY-RUN`
- `SEC-GUARD-OWNERSHIP-BACKFILL-EVIDENCE-GATES`
- `SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-DESIGN-DISABLED`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-ONLY`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PLAN-SIGNOFF`
- `SEC-GUARD-OWNERSHIP-BACKFILL-TEST-ONLY-APPLY-REHEARSAL`
- `SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-READINESS`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PRODUCTION-APPLY-PROPOSAL`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-APPLY-RUNBOOK`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-APPLY-EXECUTOR-DESIGN`
- `SEC-GUARD-OWNERSHIP-BACKFILL-STAGING-POST-RUN-EVIDENCE-ACCEPTANCE`
- `SEC-GUARD-OWNERSHIP-BACKFILL-PRODUCTION-PROMOTION-READINESS`
- `SEC-GUARD-OWNERSHIP-BACKFILL-MANUAL-WRITE-PATH-DECISION`
- `SEC-GUARD-OWNERSHIP-BACKFILL-APPLY-ARCHITECTURE-REVIEW`

## Enforcement model

- Use hard automated tests for high-confidence checks with low false-positive risk.
- Use explicit allowlists (with reasons) where discovery scans would otherwise be noisy.
- Keep fragile checks as documented-only TODO items until reliable automation is possible.
- Avoid hidden broad scans without reviewable scope or explainable failure output.

## Current guardrails

- Route inventory completeness and controller coverage checks (`ApiEndpointProtectionInventoryGuardTests`) including pilot status tracking in `docs/security/api-endpoint-protection-inventory.json` (for example `DevelopmentDemoDataController` in `AuthPilot` stage).
- Route inventory guardrails also track P5-10 read-pilot entries for `ProjectsController`/`BuildingsController` read endpoints and prevent unnoticed drift between protected and unprotected route groups.
- Route inventory guardrails also track P5-11 write-pilot entries for `ProjectsController`/`BuildingsController` write endpoints to prevent accidental unprotected mutating routes.
- Route inventory guardrails also track P5-12 execution-pilot entries for workflow/calculation endpoints so execution protection changes require inventory updates.
- Route inventory guardrails also track P5-13 report/artifact pilot entries so report/export/artifact route protections cannot drift without inventory updates.
- Route inventory guardrails also track P5-14 workflow read/history pilot entries so workflow state/scenario/job read protections cannot drift without explicit inventory updates.
- Tenant isolation matrix guardrails track cross-tenant expectations in `docs/security/tenant-isolation-integration-matrix.md` and verify representative P5-10 through P5-14 endpoint groups via `TenantIsolationAuthorizationGateMatrixTests`, `TenantIsolationProtectedEndpointSmokeTests`, `TenantIsolationAntiEnumerationTests`, and `TenantIsolationEndpointInventoryCoverageTests`.
- Persistence-backed ownership guardrails verify `docs/security/persistence-backed-tenant-ownership-fields.md`, the append-only `AddProjectTenantOwnershipFields` migration, and model snapshot ownership fields via `P5PersistenceBackedTenantOwnershipGovernanceTests`.
- Tenant-aware query isolation guardrails verify `docs/security/tenant-aware-query-isolation-services.md`, `TenantQueryIsolationPolicyTests`, `ProjectTenantScopedReadServiceTests`, `BuildingTenantScopedReadServiceTests`, and `TenantScopedReadServiceMatrixTests`.
- Tenant-aware read controller integration guardrails verify `docs/security/tenant-aware-read-controller-integration.md`, Project/Building matrix metadata in `docs/security/tenant-isolation-integration-matrix.json`, and `ProtectedReadControllersTenantScopedQueryIntegrationTests`.
- Workflow tenant-aware read integration guardrails verify `docs/security/workflow-tenant-aware-read-integration.md`, workflow matrix metadata in `docs/security/tenant-isolation-integration-matrix.json`, and `ProtectedWorkflowReadControllersTenantScopedQueryIntegrationTests`.
- Workflow ownership metadata coverage guardrails verify `docs/security/workflow-ownership-metadata-coverage.md`, `docs/security/workflow-ownership-metadata-coverage.json`, and workflow metadata coverage fields in `docs/security/tenant-isolation-integration-matrix.json`.
- Ownership backfill strategy/evidence guardrails verify `docs/security/ownership-backfill-strategy.md`, `docs/security/ownership-backfill-strategy.json`, `docs/security/ownership-backfill-evidence-model.md`, `docs/security/ownership-backfill-evidence.schema.json`, and roadmap/guardrail linkage.
- Ownership backfill dry-run tool guardrails verify `tools/AssistantEngineer.Tools.OwnershipBackfill`, no apply implementation, no write-oriented persistence calls, artifact ignore rules, and roadmap/guardrail linkage.
- Ownership backfill database dry-run guardrails verify read-only scanner presence, no-write scanner source constraints, and P6-02 documentation/roadmap linkage.
- Ownership backfill evidence-gate guardrails verify `validate-evidence` behavior, gate artifact ignore rules, threshold-policy documentation, and P6-03 roadmap linkage.
- Ownership backfill apply-design guardrails verify apply command remains disabled, apply design docs/json/schema stay aligned, and no write-path indicators appear in tool source.
- Ownership backfill plan-only guardrails verify `plan-apply` deterministic artifact generation stays no-write, apply remains disabled, plan artifact ignore rules stay active, and roadmap linkage includes P6-05.
- Ownership backfill plan-signoff guardrails verify `signoff-plan` deterministic sign-off artifact generation stays no-write, plan hash verification is enforced, apply remains disabled, signoff artifact ignore rules stay active, and roadmap linkage includes P6-06.
- Ownership backfill test-only apply rehearsal guardrails verify rehearsal executor behavior stays test-only, production apply remains disabled, rehearsal artifact ignore rules stay active, and roadmap linkage includes P6-07.
- Ownership backfill apply-readiness guardrails verify `validate-apply-readiness` artifact-chain hash consistency (`PlanHash`, sign-off hash, `ApplyInputHash`), sign-off TTL checks, previous-values completeness checks, readiness artifact ignore rules, and roadmap linkage includes P6-08 while apply remains disabled.
- Ownership backfill production-apply proposal guardrails verify proposal/template documents, machine-readable descriptors, required approval/go-no-go policy fields, `ApplyInputHash` change-management linkage, and roadmap linkage includes P6-09 while apply remains disabled.
- Ownership backfill staging-apply runbook guardrails verify staging runbook/checklist design artifacts, operator/environment/backup/rollback/promotion controls, `ApplyInputHash` staging evidence requirements, and roadmap linkage includes P6-10 while apply remains disabled.
- Ownership backfill staging-apply executor-design guardrails verify staging preflight contracts, disabled executor behavior, environment hard-deny rules for non-staging, no-write source constraints, and roadmap linkage includes P6-11 while apply remains disabled.
- Ownership backfill staging post-run evidence acceptance guardrails verify deterministic post-run evidence contract artifacts, `validate-staging-acceptance` command availability, staging acceptance artifact ignore rules, no-write source constraints, and roadmap linkage includes P6-12 while staging and production apply remain disabled.
- Ownership backfill production promotion readiness guardrails verify production promotion readiness docs/json/schema, `validate-production-promotion` command availability, production-promotion decision artifact ignore rules, no-write source constraints, and roadmap linkage includes P6-13 while staging and production apply remain disabled.
- Ownership backfill manual write-path decision guardrails verify manual decision docs/templates/json/schema, required hash/approval policy bindings, inventory and guardrail linkage, and no false claims about write-path enablement or backfill execution in P6-14.
- Ownership backfill apply-enablement architecture-review guardrails verify P6-15 architecture invariants/checklist artifacts, source-level no-wiring checks, disabled apply boundary regression checks, and roadmap linkage while staging and production apply remain disabled.
- Development/demo endpoint environment-gating checks (`DevelopmentEndpointSecurityGuardTests`).
- Secret logging/source high-confidence leakage checks (`SecretLoggingSecurityGuardTests`).
- Authentication default compatibility and secret-free appsettings checks (`ApiAuthenticationDefaultsGuardTests`).
- Rate-limiting default compatibility and partition-key safety checks (`ApiRateLimitingDefaultsGuardTests`).
- Audit metadata sanitization and payload safety registry checks (`AuditLogSecurityGuardTests`).
- InMemory provider production-risk visibility and configuration guard checks (`InMemoryProductionProviderGuardTests`).
- False security/compliance claim scan in security/frontend docs (`SecurityFalseClaimsGuardTests`).
- Frontend token/api-key hardcoding guard checks (`FrontendSecretsGuardTests`).

## Future guardrails

- Real anonymous/protected endpoint integration tests after staged route protection rollout.
- Deeper tenant isolation tests after persistence-layer ownership fields and row-level query filters are introduced.
- API-key persistence lifecycle and rotation regression tests.
- Distributed rate-limiting integration tests.
- Durable audit storage retention and tamper-evidence regression tests.
