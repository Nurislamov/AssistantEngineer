# Rate Limiting Foundation

## Purpose

This document defines a staged rate-limiting foundation for AssistantEngineer API to support production SaaS hardening without breaking current compatibility paths.

## Scope

This foundation covers:

- anonymous and IP-based requests;
- API-key requests;
- authenticated user requests;
- organization/tenant requests;
- endpoint-category limits;
- heavy workflow/calculation/report operations;
- development compatibility;
- future distributed rate-limiting evolution.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full distributed rate limiting claim.
- No full multi-tenant isolation claim yet.
- No external Redis/distributed limiter integration claim.
- No certified/certification claim.

## Partition key model

Partition priority:

1. `OrganizationId` when authenticated principal organization is available.
2. `UserId` when authenticated principal user is available.
3. `ApiKeyFingerprint` when API key header is present and fingerprintable.
4. `IpAddress`.
5. `AnonymousUnknown`.

Rules:

- Raw API keys must never be used as partition keys.
- Only fingerprints/hashes are allowed for API key partitioning.
- Raw keys must never be logged.

## Endpoint category model

Rate-limiting categories:

- `PublicRead`
- `ReferenceData`
- `ProjectRead`
- `ProjectWrite`
- `BuildingRead`
- `BuildingWrite`
- `WorkflowRead`
- `WorkflowExecute`
- `CalculationRun`
- `ReportGenerate`
- `ArtifactRead`
- `ArtifactWrite`
- `Administration`

## Default compatibility mode

- Foundation options are disabled by default for compatibility.
- Existing runtime paths continue to use current API hardening behavior unless explicit `ApiRateLimiting:Enabled=true`.
- Development environments may keep relaxed limits during staged rollout.

## Recommended initial limits

Suggested initial policy values (recommendations, not certification claims):

- Anonymous/IP:
  - `PublicRead`: 120 requests/minute
  - `CalculationRun`: 10 requests/minute
  - `WorkflowExecute`: 5 requests/minute
  - `ReportGenerate`: 5 requests/minute
- Authenticated user:
  - `PublicRead`: 600 requests/minute
  - `CalculationRun`: 60 requests/minute
  - `WorkflowExecute`: 30 requests/minute
  - `ReportGenerate`: 20 requests/minute
- Organization:
  - `CalculationRun`: 300 requests/minute
  - `WorkflowExecute`: 120 requests/minute
  - `ReportGenerate`: 60 requests/minute

## Failure behavior

- Exceeded limit returns `429 Too Many Requests`.
- Do not disclose other tenant/user traffic details.
- Use `Retry-After` headers when framework/runtime policy supports them.

## Observability and audit

- Log partition type, endpoint category, decision outcome, and correlation identifiers.
- Never log raw API keys or tokens.
- Security-significant repeated limit violations may be audited in future (`docs/security/audit-log-foundation.md`).

## Future distributed model

- Distributed limiter provider (Redis/DB-backed) for multi-node operation.
- Organization plan quotas and billing-aware limits.
- Burst vs sustained policies.
- Administrative override and emergency throttling.

Rate-limiting default and partition-key regression guardrails are tracked in `docs/security/security-regression-guardrails.md`.
P5-12 execution-endpoint authorization rollout aligns workflow/calculation protected routes with `WorkflowExecute` and `CalculationRun` category governance (`docs/security/protected-execution-endpoints-rollout.md`).
P5-13 report/artifact authorization rollout aligns protected report/artifact routes with `ReportGenerate`, `ArtifactRead`, and `ArtifactWrite` category governance (`docs/security/protected-report-artifact-endpoints-rollout.md`).
