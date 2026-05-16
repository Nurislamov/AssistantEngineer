# API Authentication Boundary

## Purpose

This document defines the staged API authentication boundary strategy for Production SaaS readiness without forcing an immediate full authorization rollout.

## Scope

This boundary covers:

- API key authentication;
- future JWT/OIDC authentication;
- development compatibility mode;
- principal context extraction;
- authentication failure behavior;
- secret handling;
- logging and observability;
- Swagger/OpenAPI disclosure boundaries;
- migration path to route authorization.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that API keys alone are complete user authentication.

## Authentication model

Staged model:

- Stage 1: optional API key boundary with development compatibility.
- Stage 2: authenticated principal context extraction boundary.
- Stage 3: route authorization policy rollout.
- Stage 4: JWT/OIDC or external provider integration.
- Stage 5: tenant isolation enforcement at route and persistence boundaries.

## API key policy

- API keys are secrets.
- When persisted in future, store hash/fingerprint rather than raw key values.
- Compare key material using safe comparison.
- Never log full key values.
- API keys may be fingerprinted for rate limiting; raw key material must not be used as a limiter partition key (`docs/security/rate-limiting-foundation.md`).
- API key ownership will move to user/organization scope in future P5 stages.
- API key permission scope remains a future item.

## JWT/OIDC future policy

- Future external subject values map to `User.ExternalSubjectId`.
- Provider choice is intentionally deferred.
- No external provider integration is implemented in this step.

## Development compatibility

- Development mode can run in anonymous compatibility while rollout is incomplete.
- Production target should require authenticated principal on protected routes.
- Behavior is controlled by `ApiAuthentication` options.

## Failure behavior

- Missing credentials on protected endpoints should return `401 Unauthorized`.
- Authenticated principal without required permission should return `403 Forbidden`.
- Invalid API key should return `401 Unauthorized`.
- Failure messages must not disclose secret validation internals.

## Observability

- Log authentication outcome status and safe principal identifiers when available.
- Never log API keys, JWTs, secrets, or raw credential payloads.
- Include correlation id context when available through request logging pipeline.
- Authentication success/failure actions can emit audit references through `docs/security/audit-log-foundation.md` without storing credential secrets.

## P5-04 readiness

P5-03 introduces contracts, options, validator abstraction, and principal extraction boundary. It does not perform broad route authorization rollout. P5-04 will apply authorization policies to project/building/workflow endpoints in controlled waves.

Rate limiting is a parallel control, not an authorization substitute. See `docs/security/rate-limiting-foundation.md`.

Frontend relationship: P5-07 introduces auth-shell UX foundation only (`docs/frontend/frontend-auth-shell-foundation.md`) and does not implement real login/provider integration in this step.

Security regression guardrails for this boundary are tracked in `docs/security/security-regression-guardrails.md`.
