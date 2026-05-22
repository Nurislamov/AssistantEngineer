# Workflow Controller Shell Reduction (P8-03F)

## Purpose

Reduce `EngineeringWorkflowController` main shell orchestration weight while preserving P8-03D characterization behavior.

## Scope

- Main controller-shell extraction in `EngineeringWorkflowController.cs`.
- API-local collaborator extraction for controller adapter orchestration.
- Composition-root registration cleanup and boundary-allowlist closure.

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

## Controller shell before

- Validate/prepare/report/trace/export orchestration logic was mixed directly in controller adapter actions.
- Remaining P8-03E allowlist still included API workflow service registration path.

## Controller shell after

- `Validate` and `PrepareCalculation` use API-local action service orchestration.
- Route/action attributes and signatures remain unchanged.
- Authorization gate order and response status behavior remain unchanged.

## API adapter collaborators extracted

- `IEngineeringWorkflowControllerActionService`
- `EngineeringWorkflowControllerActionService`
- `EngineeringWorkflowServiceRegistration` moved to API composition namespace/path.

## Files changed

- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/EngineeringWorkflowController.cs`
- `src/Backend/AssistantEngineer.Api/Services/Calculations/Composition/IEngineeringWorkflowControllerActionService.cs`
- `src/Backend/AssistantEngineer.Api/Services/Calculations/Composition/EngineeringWorkflowControllerActionService.cs`
- `src/Backend/AssistantEngineer.Api/Services/Calculations/Composition/EngineeringWorkflowServiceRegistration.cs`
- `src/Backend/AssistantEngineer.Api/Configuration/ApiPresentationRegistration.cs`

## Public API compatibility

- No route template changes.
- No action-name/signature changes.
- No DTO contract changes.
- No status-code contract changes.

## Workflow behavior compatibility

- Characterization baseline remains preserved for workflow execution/read/report/artifact behavior.
- Authorization interaction semantics remain unchanged.

## Characterization tests preserved

- `EngineeringWorkflowControllerRouteSignatureTests`
- `EngineeringWorkflowControllerCharacterizationTests`
- `EngineeringWorkflowControllerAuthorizationCharacterizationTests`
- `EngineeringWorkflowControllerResponseShapeTests`
- `P8WorkflowControllerShellCharacterizationTests`

## Allowlist impact

- Removed: `src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/EngineeringWorkflowServiceRegistration.cs`
- `tests/fixtures/architecture/engineeringworkflow-boundary-allowlist.json` is now empty.

## Remaining controller responsibilities

- HTTP route/action boundary.
- Request DTO binding and adapter validation surface.
- Authorization gate invocation and decision mapping.
- Tenant-scoped read branching and failure-to-HTTP mapping.
- Response/result envelopes.

## Remaining limitations

- `GET /engineering-workflow/scenarios/{scenarioId}/artifacts` unresolved scenario currently returns `200` with empty list (characterized behavior).
- Unresolved workflow-id fallback limitation from P8-03A/B/C remains intentionally unchanged.

## Next steps

- P8-03 sequence can be closed with this controller-shell reduction baseline.
- Optional follow-up: P8-04 OwnershipBackfill CLI parser simplification.

