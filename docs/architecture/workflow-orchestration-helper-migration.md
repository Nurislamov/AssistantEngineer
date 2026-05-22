# Workflow Orchestration Helper Migration (P8-03E)

## Purpose

Safely migrate workflow orchestration helpers from API service shell paths into EngineeringWorkflow module application paths without changing behavior.

## Scope

- Migrate safe workflow helper implementations to `AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow`.
- Keep `EngineeringWorkflowController` as API adapter with unchanged routes/actions/status contracts.
- Preserve P8-03D characterization behavior locks.

## Non-claims

- No workflow behavior change claim.
- No public API route change claim.
- No DTO shape change claim.
- No authorization behavior change claim.
- No calculation physics change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Helpers migrated

- `EngineeringWorkflowDiagnosticsService`
- `EngineeringWorkflowStateBuilder`
- `EngineeringWorkflowSubmissionService`

## Interfaces migrated

- `IEngineeringWorkflowDiagnosticsService`
- `IEngineeringWorkflowStateBuilder`
- `IEngineeringWorkflowSubmissionService`
- `IEngineeringWorkflowScenarioRunner`
- `IEngineeringWorkflowJobService`
- `IEngineeringWorkflowPersistenceService`

## Files intentionally deferred

- `src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/EngineeringWorkflowServiceRegistration.cs` (API composition-root wiring remains in host layer).
- `EngineeringWorkflowController` shell reduction remains staged for P8-03F.

## Public API compatibility

- Controller route templates are unchanged.
- Controller action signatures are unchanged.
- Response contract guards from P8-03D remain applicable.

## Workflow behavior compatibility

- Submission/idempotency/replay/orchestration behavior remains unchanged.
- State/diagnostic derivation behavior remains unchanged.
- Authorization gate behavior remains unchanged.

## Characterization tests preserved

- P8-03D route/signature/status/response characterization suites remain active.
- Existing authorization/workflow characterization coverage remains active.

## Allowlist impact

- Removed P8-01 allowlist entries for migrated workflow helper implementations and companion interfaces.
- Kept API composition-root allowlist entry for `EngineeringWorkflowServiceRegistration.cs` (P8-03F).

## Next steps

- P8-03F: reduce `EngineeringWorkflowController` partial shell with route/action/signature compatibility preserved.
