# API Endpoint Classification Model

## Purpose

This document defines the canonical classification model for `docs/security/api-endpoint-protection-inventory.json` so route-governance automation can detect inventory drift and claim drift early.

## Endpoint groups

- `PublicRead`
- `PublicWrite`
- `ProtectedRead`
- `ProtectedWrite`
- `WorkflowRead`
- `WorkflowExecute`
- `CalculationRun`
- `ReportsRead`
- `ReportsWrite`
- `ArtifactRead`
- `ArtifactWriteDeferred`
- `AdminDeferred`
- `HealthDiagnostics`
- `UnknownNeedsClassification`

## Protection stages

- `P5-09`
- `P5-10`
- `P5-11`
- `P5-12`
- `P5-13`
- `P5-14`
- `P5-15`
- `P5-16C`
- `P5-16D`
- `Deferred`
- `Compatibility`
- `Public`
- `UnknownNeedsClassification`

## Required fields

Every inventory endpoint entry should include:

- `route`
- `httpMethod`
- `controller`
- `action`
- `endpointGroup`
- `protectionStage`
- `permission`
- `tenantScope`
- `rateLimitCategory`
- `auditCategory`
- `publicClaim`
- `knownLimitations`

## Auth category

- `permission` must represent the staged authorization target for the route.
- `Public`/compatibility routes can use `NotApplicable` with explicit `knownLimitations`.
- Protected groups must not use empty permission values.

## Rate limit category

- `rateLimitCategory` must align with `EndpointRateLimitCategories` and `docs/security/rate-limiting-foundation.md`.
- Execution-sensitive routes should use execution-oriented categories (`WorkflowExecute`, `CalculationRun`) with clear notes.

## Audit category

- `auditCategory` should align with `docs/security/audit-log-foundation.md` categories.
- Transitional routes can use `AuditDeferred` only with explicit `knownLimitations`.

## Tenant scope category

Allowed tenant-scope vocabulary:

- `Project`
- `Building`
- `Workflow`
- `Scenario`
- `Job`
- `Artifact`
- `TenantScoped`
- `LegacyUnscopedAllowed`
- `Deferred`
- `NotApplicable`
- `UnknownNeedsClassification`

## Claims boundary

- Inventory is staged governance evidence, not a claim of fully complete tenant isolation.
- Inventory must not claim that production apply is active, ownership backfill is already executed, DB row-level security is active, or global EF query filters are active.
- Canonical capability claims remain in `docs/security/security-release-boundary.md`.

## Known limitations

- Route discovery automation is text-level and not a full runtime endpoint graph.
- Some inventory entries intentionally aggregate multiple methods (`MULTI`) and require staged refinement.
- Deferred and unknown classifications are allowed only with explicit limitations.

## Non-claims

- No production security certification claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No certified/certification claim.
