# Protected Endpoint Authorization Gate Decomposition (P8-03B/P8-03C)

## Purpose

Track staged internal decomposition of `ProtectedEndpointAuthorizationGate` while keeping facade and behavior stable.

## Scope

- P8-03B extraction of decision factory, tenant mismatch policy, and authorization logger collaborators.
- P8-03C extraction of permission evaluator and scope evaluation service collaborators.
- No route/API/DTO/calculation behavior changes.

## Non-claims

- No authorization behavior change claim.
- No public API route change claim.
- No DTO shape change claim.
- No calculation physics change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Stage status

- P8-03A: Implemented (characterization matrix freeze).
- P8-03B: Implemented (decision/logging/tenant-mismatch collaborators extracted).
- P8-03C: Implemented (permission/scope evaluation collaborators extracted).

## Components extracted

- `IProtectedEndpointAuthorizationDecisionFactory`
- `IProtectedEndpointTenantMismatchPolicy`
- `IProtectedEndpointAuthorizationLogger`
- `IProtectedEndpointPermissionEvaluator`
- `IProtectedEndpointScopeEvaluationService`

## Remaining responsibilities in gate facade

- Capability-specific option-gate branching and fallback ordering orchestration.
- Public facade method dispatch and compatibility-safe decision flow composition.
- No direct resolver/policy primitive implementation logic remains in facade.

## Compatibility contract

- Keep `IProtectedEndpointAuthorizationGate` method signatures unchanged.
- Keep decision names unchanged: `Allowed`, `Unauthorized`, `Forbidden`, `NotFound`.
- Keep status mapping unchanged: `200`, `401`, `403`, `404`.
- Keep anti-enumeration mapping semantics unchanged.

## Deferred items

- P8-03D workflow controller shell characterization tests.
- P8-03E workflow orchestration helper migration to module namespace.
- P8-03F workflow controller shell size reduction.

## Verification

- Characterization tests remain required for every decomposition step.
- Full solution build/test, release-ready gate, and disabled-apply safety checks remain required.
