# Workflow Ownership Metadata Coverage

## Purpose

P5-17 hardens workflow/scenario/job tenant ownership metadata coverage for controlled tenant-aware workflow reads.

## Scope

This step inventories ownership metadata used by workflow read/history access resolution and strengthens resolver/read-service behavior where metadata is complete versus partial.

P5-17 includes:

- metadata inventory for workflow/scenario/job persistence records;
- resolver coverage inventory for workflow/scenario/job scope resolution paths;
- strict-mode handling for unresolved ownership scope in workflow tenant-aware read checks;
- tests and governance updates for metadata coverage claims.

## Non-claims

- No production security certification claim.
- No SOC 2 / ISO 27001 compliance claim.
- No full multi-tenant isolation claim yet.
- No database row-level security claim.
- No global EF query filter claim.
- No ownership backfill claim.
- No external identity provider integration claim.
- No certified/certification claim.

## Current metadata inventory

P5-17 inventory source of truth:

- `docs/security/workflow-ownership-metadata-coverage.json`
- `src/Backend/AssistantEngineer.Api/Services/Calculations/Persistence/Durable/EngineeringWorkflowPersistenceEntities.cs`
- `src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Contracts/EngineeringWorkflow/EngineeringCalculationScenarioDtos.cs`
- `src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Contracts/EngineeringWorkflow/EngineeringCalculationJobDtos.cs`

## Workflow metadata coverage

- Workflow state snapshots are keyed by project/building scope (`ProjectId`, optional `BuildingId`) and are complete for project-scoped state read resolution.
- Generic persisted `WorkflowId` ownership column is not introduced in this step.
- `ResolveWorkflowScopeAsync` still resolves by identifier lookup through scenario/job metadata, so generic workflow-id-only paths remain partial.

## Scenario metadata coverage

- Scenario records persist `ScenarioId`, `ProjectId`, optional `BuildingId`.
- This is sufficient for tenant scope derivation in protected scenario read/list paths when project ownership metadata exists.
- Scenario records do not add a separate persisted `WorkflowId` in this step.

## Job metadata coverage

- Job records persist `JobId`, `ProjectId`, `ScenarioId`.
- Job event records persist `JobId`, `ScenarioId`, `ProjectId`.
- Building scope for jobs is derived from linked scenario metadata when scenario is available.
- Missing/legacy linkage paths remain partial.

## Resolver behavior

- `ResolveScenarioScopeAsync` uses scenario metadata (`ProjectId`, `BuildingId`) then project/building scope resolvers.
- `ResolveJobScopeAsync` prioritizes scenario linkage metadata when available and falls back to job `ProjectId` scope.
- `ResolveWorkflowScopeAsync` resolves by identifier against scenario first, then job.
- Invalid/non-positive metadata identifiers are normalized to missing and do not produce synthetic ownership scope.

## Staged fallback cases

Fallback remains staged for metadata-incomplete records/paths:

- workflow identifier paths where neither scenario nor job metadata can resolve ownership;
- legacy scenario/job records lacking resolvable ownership linkage;
- project ownership not yet backfilled for historical datasets.

Strict mode denies unresolved ownership scope. Compatibility mode can allow only via explicit unscoped transition options.

## Safe metadata additions

P5-17 does not add new persistence ownership columns because current scenario/job metadata is sufficient for controlled resolver hardening and coverage proof in this step.

Potential future additions (P6+) remain optional and should be introduced only if proven necessary by unresolved production paths.

## Migration status

No migration is added in P5-17.

- No destructive schema operation.
- No ownership backfill operation.
- No historical migration edits.

## Known limitations

- Ownership backfill is still pending for legacy records.
- Generic workflow-id-only ownership resolution remains partial.
- No global EF query filters.
- No database row-level security.
- No external identity provider integration.

## Next steps

- P6: ownership backfill strategy and execution for historical workflow/scenario/job records.
- P6: re-evaluate metadata additions only where unresolved paths remain after backfill evidence.
- P6: consider stronger persistence-layer enforcement only after metadata/backfill readiness is proven.
