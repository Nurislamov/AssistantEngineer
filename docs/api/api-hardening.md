# API Hardening Baseline (P2-01)

## Scope

This document describes the P2-01 hardening baseline for `AssistantEngineer.Api`.

It is a foundation-level hardening pass and not a full production security program.

## Included in this baseline

1. API key authentication boundary from P0 remains active and unchanged.
2. In-memory fixed-window rate limiting baseline using `Microsoft.AspNetCore.RateLimiting`.
3. Anonymous operational health endpoints:
   - `GET /health` (liveness)
   - `GET /ready` (readiness)
4. Explicit CORS policy through configuration.

## Rate limiting

- Rate limiting is configured through `ApiHardening:RateLimiting`.
- Default baseline uses in-memory fixed-window limits.
- Heavy workflow endpoints use explicit named policy:
  - `POST /api/v1/engineering-workflow/run-calculation`
  - `POST /api/v1/engineering-workflow/jobs`
  - `POST /api/v1/engineering-workflow/report/export/json`
  - `POST /api/v1/engineering-workflow/report/export/markdown`
- This baseline is local/in-memory and not distributed.

## Health and readiness

- `/health` is lightweight liveness.
- `/ready` evaluates readiness checks for current application composition.
- Endpoints are anonymous to support orchestrator/load-balancer probes.
- Responses are intentionally non-sensitive and do not expose secrets or connection strings.

## CORS baseline

- CORS is explicit and configuration-driven via `ApiHardening:Cors`.
- Default `appsettings.json` is deny-by-default for origins (empty origins list).
- Development allows localhost frontend origins.
- Production origins must be provided via deployment configuration/environment variables.
- Wildcard origin (`*`) is not used in default production baseline.

## Configuration model

- `ApiHardening:Cors`
  - `Enabled`
  - `PolicyName`
  - `AllowedOrigins`
  - `AllowedMethods`
  - `AllowedHeaders`
- `ApiHardening:RateLimiting`
  - `Enabled`
  - `PermitLimit`
  - `WindowSeconds`
  - `QueueLimit`
  - `AutoReplenishment`
  - `DefaultPolicyName`
  - `HeavyPolicyName`
  - `HeavyPermitLimit`
  - `HeavyWindowSeconds`

## Known limitations after P2-01

- structured audit logging is future work.
- local in-memory idempotency is available in P2-04, but distributed/durable idempotency remains future work.
- tenant/user isolation hardening is future work.
- distributed rate limiting for multi-node deployment is future work.
- deeper readiness checks with external dependency semantics are future work.

## Non-claims

- This baseline does not claim full production security coverage.
- This baseline does not prove engineering correctness.
- This baseline is not a compliance certificate.
- This baseline is not external validation evidence.
- This baseline does not provide full standard compliance claim.
