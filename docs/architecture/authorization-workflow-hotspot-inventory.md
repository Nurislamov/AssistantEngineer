# Authorization and Workflow Hotspot Inventory (P8-03)

## Purpose

Capture current authorization/workflow hotspots and define decomposition-ready boundaries without changing behavior.

## Scope

- `ProtectedEndpointAuthorizationGate` and related scope resolvers/options/tests.
- `EngineeringWorkflowController` partial shell.
- Workflow orchestration helpers currently owned by `AssistantEngineer.Modules.EngineeringWorkflow/Application/Workflow`.
- P8-01 workflow boundary allowlist entries that remain intentionally deferred.

## Non-claims

- No authorization behavior change claim.
- No public API route change claim.
- No DTO shape change claim.
- No calculation physics change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## ProtectedEndpointAuthorizationGate responsibilities

- Protection toggle evaluation based on `ApiAuthorizationOptions`.
- Development anonymous bypass policy.
- Principal authentication and permission checks.
- Scope resolution across project/building/floor/room/workflow/scenario/job identifiers.
- Tenant-aware access policy evaluation through `ProjectTenantAccessPolicy`.
- Anti-enumeration outcome mapping (`Forbidden` vs `NotFound`) for tenant mismatch.
- Decision-to-controller outcome contract (`Allowed`/`Unauthorized`/`Forbidden`/`NotFound`).
- Structured informational logging for denied scope decisions.

## ProtectedEndpointAuthorizationGate decomposition candidates

- Protection requirement evaluator (options-to-requirement rules).
- Permission evaluator (principal permission checks).
- Scope evaluation service (resource-specific scope resolution and policy checks).
- Tenant mismatch policy component (anti-enumeration decision mapping).
- Authorization decision factory (centralized decision construction).
- Authorization logger component (uniform deny logging contract).

P8-03 status note:

- P8-03A characterization matrix is implemented.
- P8-03B and P8-03C collaborator extraction is implemented with stable facade and preserved decision/status semantics.
- P8-03D workflow controller shell characterization is implemented (route/action/status/response compatibility freeze).
- P8-03F workflow controller shell reduction is implemented for the main controller shell (validate/prepare orchestration extraction) with characterization behavior preserved.

## Workflow controller shell responsibilities

- Route adapter surface for validate, prepare/run calculation, jobs, state/history, report/artifact endpoints.
- Authorization gate invocation and decision-to-HTTP translation.
- Tenant-scoped workflow read branching and fallback handling.
- Query/paging normalization and response shaping.
- Error envelope consistency for not-found/conflict/bad-request cases.

## Workflow service decomposition candidates

- `EngineeringWorkflowSubmissionService`: mixes idempotency, replay fallback, persistence integration, and job/scenario orchestration.
- `EngineeringWorkflowStateBuilder`: large workflow-state assembly pipeline with diagnostics enrichment and preview/report summary shaping.
- `EngineeringWorkflowDiagnosticsService`: diagnostics normalization + step-status derivation + severity mapping; candidate for narrower collaborators.

P8-03E status note:

- Workflow orchestration helpers (`EngineeringWorkflowDiagnosticsService`, `EngineeringWorkflowStateBuilder`, `EngineeringWorkflowSubmissionService`) were migrated to module application paths.
- API composition-root registration remains in API layer by design, and P8-03F completes controller-shell reduction for the main shell while keeping characterized limitations unchanged.

## P8-01 allowlist relationship

- P8-01 allowlist entries remain active for API workflow shell files.
- Proposed stage alignment:
  - `P8-03E`: orchestration migration toward module application layer.
  - `P8-03F`: controller/adapter shell reduction and interface placement cleanup.

## Risk assessment

- Gate hotspot risk: high internal complexity with repeated decision flow can increase semantic-regression risk during refactor.
- Workflow shell risk: large controller partials and mixed orchestration reduce change isolation.
- Combined risk: authorization and workflow changes intersect protected endpoints; decomposition requires characterization-first sequencing.

## Proposed staged decomposition

- P8-03A: characterization tests for authorization gate decision/status matrix. Status: Implemented.
- P8-03B: internal extraction of decision factory/logger/tenant mismatch policy, gate facade unchanged.
- P8-03C: internal extraction of permission/scope evaluation collaborators, semantics unchanged.
- P8-03D: characterization tests for workflow controller shell response behavior. Status: Implemented.
- P8-03E: move safe workflow orchestration helpers out of API namespace into module application layer. Status: Implemented.
- P8-03F: reduce `EngineeringWorkflowController` partial shell while preserving route/action signatures. Status: Implemented.

## Non-goals

- No direct refactor of authorization behavior in P8-03.
- No controller route/action signature changes in P8-03.
- No DTO contract changes in P8-03.
- No persistence model/migration changes in P8-03.
- No calculation-engine or numerical behavior changes in P8-03.
