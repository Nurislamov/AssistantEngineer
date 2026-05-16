# Security Boundary Policy

## Purpose

This policy defines baseline security boundary rules for production SaaS evolution without changing current runtime behavior in a single step.

## Principal model

- All protected API operations must execute under an authenticated principal context.
- Principal types will evolve from shared API key context to user/organization-scoped principals.
- Authorization decisions must use principal claims and server-side ownership checks, not client-supplied identity fields.
- API authentication boundary strategy is documented in `docs/security/api-authentication-boundary.md`.

## Tenant boundary model

- Tenant boundary is organization-based in the target model.
- Project/building/workflow records must be associated with tenant ownership metadata before full multi-tenant rollout.
- Cross-tenant access must be denied by default unless an explicit cross-tenant admin policy exists.

## Project ownership rule

- Project resources must be owned by a principal and tenant context.
- Building and workflow resources inherit project ownership boundaries.
- Ownership checks are required for both read and write operations.
- P5-02 policy foundation is documented in `docs/security/project-tenant-scoping-model.md`.
- Tenant match rule: principal organization id must match resource organization id when strict matching is enabled.
- Legacy unscoped resources are migration risk and must be retired through staged ownership backfill.

## Controller authorization rule

- Controllers must not trust route/query/body identifiers as authorization proof.
- Access must be authorized against the authenticated principal and ownership context.
- Anonymous/public routes must be explicitly marked and documented.
- P5-03 introduces authentication boundary foundation; broad route authorization remains a P5-04 step.

## Development-only endpoint rule

- Any development/demo endpoint must be environment-gated and non-operational in production.
- Environment gating checks must be covered by guard tests.
- New development endpoints require explicit review for accidental exposure.

## API key handling rule

- API keys are credentials and must be treated as secrets.
- API keys must not be logged, echoed, or returned by API responses.
- API keys are not a complete substitute for principal identity/authorization and must evolve toward scoped credentials.
- API-key fingerprints (not raw keys) may be used for scoped rate limiting as documented in `docs/security/rate-limiting-foundation.md`.

## Secret/logging rule

- Never log raw secrets, API keys, tokens, passwords, or full sensitive payloads.
- Log references and identifiers rather than full large JSON payloads.
- Structured logging must prefer stable event codes and explicit metadata fields.

## Audit event rule

- Security-relevant actions must emit auditable events with principal, scope, timestamp, and action outcome.
- Audit trails must avoid full engineering payload storage in log fields.
- Immutable or append-only audit persistence is required for production-grade forensic traceability.
- Audit foundation and event taxonomy are documented in `docs/security/audit-log-foundation.md`.

## Rate limiting rule

- Rate limits must support scoping by user, organization, API key, and IP where applicable.
- Coarse global limits are acceptable as transitional baseline but not final SaaS isolation.
- Heavy workflow endpoints require stricter policies and explicit monitoring.
- Staged foundation and compatibility defaults are documented in `docs/security/rate-limiting-foundation.md`.

## Testing rule

- Add regression tests for:
  - anonymous access boundaries;
  - principal ownership enforcement;
  - tenant isolation behavior;
  - development endpoint gating;
  - secret-safe logging constraints.
- Keep provider-neutral tests in default runs; optional provider-specific integration remains opt-in when needed.
- Security regression guardrail baseline is documented in `docs/security/security-regression-guardrails.md`.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
