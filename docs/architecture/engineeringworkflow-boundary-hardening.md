# EngineeringWorkflow Boundary Hardening (P8-01)

## Purpose

Fix EngineeringWorkflow namespace/layer leakage identified in P8-00 without changing runtime behavior, calculation physics, public routes, or DTO payload shapes.

## Scope

- EngineeringWorkflow module namespace normalization.
- API/module using cleanup after namespace normalization.
- Architecture guardrails for EngineeringWorkflow namespace boundaries.
- Documentation and inventory alignment for P8-01.

## Non-claims

- No calculation physics change claim.
- No public API route change claim.
- No DTO shape change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Findings addressed

- P8-00-F01 (High): EngineeringWorkflow application namespace leak in `AssistantEngineer.Api.*` references from module application code.

## Files/namespaces changed

- `src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Contracts/EngineeringWorkflow/*`
  - `AssistantEngineer.Api.Contracts.Calculations` -> `AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow`
- `src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Workflow/*`
  - `AssistantEngineer.Api.Services.Calculations.Workflow` -> `AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow`
- `src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Idempotency/*`
  - `AssistantEngineer.Api.Services.Calculations.Idempotency` -> `AssistantEngineer.Modules.EngineeringWorkflow.Application.Idempotency`
- `src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Jobs/*`
  - `AssistantEngineer.Api.Services.Calculations` -> `AssistantEngineer.Modules.EngineeringWorkflow.Application.Jobs`
- `src/Backend/AssistantEngineer.Modules.EngineeringWorkflow/Application/Persistence/IEngineeringCalculationJobEventRepository.cs`
  - `AssistantEngineer.Api.Services.Calculations.Persistence` -> `AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence`
- API/tests updated to reference new module namespaces for moved EngineeringWorkflow contracts/services.

## Files intentionally not changed

- `src/Backend/AssistantEngineer.Api/Controllers/Calculations/EngineeringWorkflowController*.cs`
- `src/Backend/AssistantEngineer.Api/Services/Calculations/Workflow/*` (API workflow shell retained with explicit allowlist and staged defer)

## Public API compatibility

- Routes unchanged.
- Action signatures unchanged.
- Public response/request shapes unchanged.

## Calculation physics compatibility

- No formulas changed.
- No numerical expected values changed.
- No scenario/solver semantics changed.

## Deferred work

- API workflow shell decomposition and controller hotspot reduction remain deferred to P8-03.
- Module-boundary coverage expansion for EngineeringWorkflow assembly remains planned for P8-02.

## Verification

- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\\scripts\\engineering-core\\assert-engineering-core-v1-release-ready.ps1`
- `dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA --evidence .\\artifacts\\ownership-backfill\\dry-run-local --gate-result .\\artifacts\\ownership-backfill\\gate-local --output .\\artifacts\\ownership-backfill\\apply-local --database-provider SQLite --connection-string \"Data Source=fake.db;Password=super-secret\"`
