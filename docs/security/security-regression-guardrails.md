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
