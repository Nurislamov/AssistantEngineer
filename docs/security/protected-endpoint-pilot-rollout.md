# Protected Endpoint Pilot Rollout

## Purpose

This document records the first controlled protected-endpoint pilot rollout for P5-09 without broad route lock-down.

## Scope

This pilot covers:

- one development/demo endpoint group only;
- options-driven pilot authorization toggle;
- integration-test validation for 401/403/success flows;
- preservation of development environment gating;
- inventory and governance synchronization.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No external identity provider integration claim.
- No certified/certification claim.
- No claim that all API endpoints are protected yet.

## Selected pilot endpoint

- Controller: `DevelopmentDemoDataController`
- Route group: `api/v{version:apiVersion}/development/demo-data`
- Action: `POST /seed`
- Pilot policy: `AdministrationManage`
- Inventory status: `AuthPilot` (`docs/security/api-endpoint-protection-inventory.json`)

## Protection behavior

- Pilot authorization is enforced only when:
  - `ApiAuthorization:Enabled=true`; and
  - `ApiAuthorization:EnableEndpointProtectionPilot=true`.
- With pilot enabled:
  - unauthenticated principal returns `401`;
  - authenticated principal without `AdministrationManage` returns `403`;
  - authenticated principal with `AdministrationManage` can proceed.
- With pilot disabled:
  - endpoint behavior remains compatibility-safe.

## Compatibility defaults

- `ApiAuthorization:Enabled=false`
- `ApiAuthorization:EnableEndpointProtectionPilot=false`
- `ApiAuthorization:AllowAnonymousInDevelopment=true`

Defaults preserve local/dev compatibility and avoid accidental mass route protection.

## Environment gate preservation

- `DevelopmentDemoDataController` keeps existing `IsDevelopment()` gate.
- In non-development environments the endpoint still returns `404 NotFound`.
- Pilot authorization does not replace development-only gate behavior.

## Test matrix

- Pilot disabled preserves existing behavior.
- Pilot enabled + missing credentials => `401`.
- Pilot enabled + authenticated principal without permission => `403`.
- Pilot enabled + authenticated principal with `AdministrationManage` => success.
- Production environment remains `404` for development endpoint even with valid principal.
- Failure responses do not disclose secrets/auth internals.

## What is intentionally not protected yet

- No mass rollout across projects/buildings/workflows/calculations/report endpoints.
- No global fallback policy change for full route authorization.
- No tenant-isolation enforcement rollout.
- No external identity provider integration.

## Next rollout candidates

- read-only `Projects` routes with `ProjectsRead` under staged scoping checks;
- read-only report endpoints (`ReportsRead`) after principal/project ownership wiring;
- workflow execution endpoints only after stronger integration and tenant-scope coverage.

P5-10 follow-up note:

- P5-10 expands controlled rollout to read-only `Projects`/`Buildings` endpoints with option-gated pilot checks.
- See `docs/security/protected-read-endpoints-rollout.md` for detailed behavior and test coverage.
