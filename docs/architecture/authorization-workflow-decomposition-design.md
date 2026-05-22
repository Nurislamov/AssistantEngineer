# Authorization and Workflow Decomposition Design (P8-03)

## Purpose

Define a safe decomposition design for authorization/workflow hotspots while preserving existing runtime behavior.

## Scope

- `ProtectedEndpointAuthorizationGate` internal decomposition target shape.
- `EngineeringWorkflowController` and API workflow shell decomposition target shape.
- Staged execution plan for characterization-first refactoring.

## Non-claims

- No authorization behavior change claim.
- No public API route change claim.
- No DTO shape change claim.
- No calculation physics change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Design principles

- Keep externally consumed API/controller contracts unchanged.
- Preserve authorization outcome semantics and precedence.
- Extract by responsibility seams with behavior characterization before each extraction.
- Keep composition-root clarity explicit when moving orchestration helpers.
- Prefer internal collaborator interfaces over large cross-layer facade expansion.

## ProtectedEndpointAuthorizationGate target shape

- Keep `IProtectedEndpointAuthorizationGate` as stable public facade.
- Introduce internal focused collaborators (future stages):
  - `IProtectedEndpointPermissionEvaluator`
  - `IProtectedEndpointScopeEvaluationService`
  - `IProtectedEndpointDecisionFactory`
  - `IProtectedEndpointAuthorizationLogger`
  - `IProtectedEndpointTenantMismatchPolicy`
- Retain current decision outcomes and fallback ordering:
  - option gate -> dev anonymous bypass -> principal authentication -> permission check -> scope evaluation -> anti-enumeration mapping.

## Workflow API/controller target shape

- Keep controller routes and action signatures unchanged.
- Keep controllers as API adapters:
  - request adaptation/validation envelope;
  - authorization call + outcome mapping;
  - application orchestration dispatch;
  - response envelope mapping.
- Reduce controller shell by extracting local adapter helpers and moving orchestration concerns where safe.

## EngineeringWorkflow application target shape

- Move safe orchestration helpers currently under `AssistantEngineer.Api/Services/Calculations/Workflow` into module application namespace:
  - `AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow`.
- Keep composition root wiring in API layer where appropriate.
- Ensure no reverse dependency from module application to `AssistantEngineer.Api`.

## Logging/audit boundary

- Preserve existing deny-path log semantics and message intent.
- Centralize authorization deny logging behind collaborator abstraction in P8-03B.
- Keep structured fields for resource identifiers, permission, and not-found policy indicator.

## Tenant-scope resolution boundary

- Keep resolver interfaces (`IProjectReadAccessScopeResolver`, `IBuildingReadAccessScopeResolver`, `IFloorAccessScopeResolver`, `IRoomAccessScopeResolver`, `IWorkflowAccessScopeResolver`) as boundary inputs.
- Preserve current resource-fallback ordering per endpoint category.
- Keep null-scope behavior and not-found mapping unchanged.

## Permission decision boundary

- Preserve permission checks against principal permission set exactly as today.
- Preserve read/write/workflow/report/artifact pilot-option gating semantics.
- Preserve distinction between `RequirePermissionAsync` and scoped authorization methods.

## Anti-enumeration boundary

- Preserve `ReturnNotFoundForTenantMismatch` and `ReturnNotFoundForWorkflowTenantMismatch` behavior and precedence.
- Preserve current conditions where workflow scope falls back to broader scope checks.
- Preserve `Forbidden` versus `NotFound` outcomes for characterization matrix cases.

## Backward compatibility requirements

- No changes to public routes or route versioning.
- No changes to action signatures.
- No changes to DTO JSON shape.
- No changes to authorization semantics/outcome mapping.
- No changes to calculation physics or numerical behavior.
- No changes to ownership backfill/apply boundaries.

## Proposed stages

- P8-03A: authorization gate characterization tests (freeze matrix). Status: Implemented in this stage.
- P8-03B: extract decision factory/logger/tenant mismatch policy. Status: Implemented in this stage chain.
- P8-03C: extract permission/scope evaluation collaborators. Status: Implemented in this stage.
- P8-03D: workflow API shell characterization tests (freeze controller behavior). Status: Implemented in this stage.
- P8-03E: move safe orchestration helpers from API namespace to module application namespace. Status: Implemented in this stage.
- P8-03F: reduce `EngineeringWorkflowController` partial shell. Status: Implemented in this stage.

## Verification strategy

- For each stage, run full architecture and API tests plus gate-focused suites.
- Add characterization snapshots/matrices before internal extraction.
- Require no diff in protected endpoint outcomes for seeded scenarios.
- Keep release-ready gate and disabled-apply checks green after each stage.

## Deferred items

- Gate collaborator extraction (P8-03B/P8-03C) is implemented with stable facade and characterization coverage.
- Workflow API shell behavior characterization (P8-03D) is implemented via route/signature/status/response guards.
- Workflow orchestrator helper relocation (P8-03E) is implemented for diagnostics/state/submission helpers and companion interfaces.
- P8-03 sequence is completed through P8-03F shell reduction; any further controller decomposition is optional follow-up.
