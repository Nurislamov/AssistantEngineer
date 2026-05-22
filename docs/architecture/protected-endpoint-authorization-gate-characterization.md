# Protected Endpoint Authorization Gate Characterization (P8-03A)

## Purpose

Freeze the current `ProtectedEndpointAuthorizationGate` decision/status behavior before any internal decomposition refactor.

## Scope

- Characterize outcome matrix for protected capabilities.
- Freeze option-controlled behavior and anti-enumeration mapping.
- Record known limitations/surprising behaviors without changing runtime semantics.

## Non-claims

- No authorization behavior change claim.
- No public API route change claim.
- No DTO shape change claim.
- No calculation physics change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Current gate responsibilities

- Protection requirement evaluation from `ApiAuthorizationOptions`.
- Development anonymous bypass handling.
- Principal authentication and permission checks.
- Scope resolution for project/building/floor/room/workflow/scenario/job identifiers.
- Tenant-policy evaluation and anti-enumeration outcome mapping.
- Deny-path structured logging.

## Characterized decision matrix

- Outcome values are frozen as:
  - `Allowed`
  - `Unauthorized`
  - `Forbidden`
  - `NotFound`
- Capability coverage freeze includes:
  - `ProjectsRead`
  - `ProjectsWrite`
  - `BuildingsRead`
  - `BuildingsWrite`
  - `WorkflowsRead`
  - `WorkflowsExecute`
  - `CalculationRun`
  - `ReportsRead`
  - `ReportsWrite`
  - `ArtifactRead`

## Options-controlled behavior

- When authorization is disabled (`ApiAuthorization.Enabled=false`), protected capability checks remain compatibility-allowed.
- Protection behavior remains gated by pilot+require flags per capability group (read/write/execution/report-artifact/workflow-read).

## Authentication behavior

- With protection enabled, unauthenticated principals return `Unauthorized` for covered capabilities.

## Permission behavior

- With protection enabled, authenticated principals missing required permission return `Forbidden`.

## Scope resolution behavior

- Missing `Project`/`Building` scopes return `NotFound`.
- Missing `Room`/`Floor` scope in calculation checks returns `NotFound`.
- Workflow/report paths with unresolved workflow scope and no project/building fallback currently return `Allowed` (documented limitation; unchanged in P8-03A).

## Tenant mismatch behavior

- `ReturnNotFoundForTenantMismatch=false` -> cross-tenant mismatch returns `Forbidden`.
- `ReturnNotFoundForTenantMismatch=true` -> cross-tenant mismatch returns `NotFound`.
- `ReturnNotFoundForWorkflowTenantMismatch=true` (or global not-found option enabled) -> workflow mismatch returns `NotFound`.

## Anti-enumeration behavior

- Deny outcome mapping remains configurable between `Forbidden` and `NotFound`.
- Decision names remain generic outcome names and do not encode tenant ownership details.

## Logging/observability expectations

- Deny paths emit structured informational logs.
- Characterization checks assert no secret-like values (`Password`, raw connection-string fragments) appear in deny diagnostics.

## Refactor safety contract

- Preserve decision precedence and status mapping:
  - option gate -> development bypass -> authentication -> permission -> scope/policy -> anti-enumeration mapping.
- Preserve outcome names and status-code mapping expectations.
- Preserve route/DTO behavior (no API contract drift).

## Known limitations

- Workflow/report checks may allow when a workflow identifier is provided but scope cannot be resolved and no fallback identifiers are provided.
- Characterization intentionally records this behavior and does not change it in P8-03A.

## Next steps

- P8-03B and P8-03C collaborator extraction are implemented under behavior-lock tests.
- Proceed to P8-03D workflow controller shell characterization before workflow shell refactor stages.
