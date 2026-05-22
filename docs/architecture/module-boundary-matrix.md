# Module Boundary Matrix (P8-02)

## Purpose

Define and enforce explicit dependency-direction boundaries across API, modules, infrastructure, tools, and tests.

## Scope

- Backend runtime assemblies under `src/Backend`.
- Tooling assemblies under `tools/AssistantEngineer.Tools.*`.
- Architecture tests that validate project-reference direction and namespace direction.
- Explicit inclusion of `AssistantEngineer.Modules.EngineeringWorkflow` in shared module-boundary coverage.

## Non-claims

- No calculation physics change claim.
- No public API route change claim.
- No DTO shape change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Matrix summary

The canonical machine-readable matrix is `docs/architecture/module-boundary-matrix.json`.

High-level direction:

- API layer may reference modules and infrastructure as composition root.
- Domain/application modules may reference other domain modules and shared kernel only when listed in matrix.
- Modules must not reference `AssistantEngineer.Api` or `AssistantEngineer.Tools.*`.
- Infrastructure references modules/shared kernel per matrix and is not referenced by modules.
- Tools can reference runtime assemblies only if explicitly allowed by matrix and remain outside runtime reverse dependencies.
- Tests can reference runtime and tool assemblies for verification.

## EngineeringWorkflow coverage

`AssistantEngineer.Modules.EngineeringWorkflow` is now explicitly represented in matrix entries and covered by:

- `ModuleBoundaryMatrixTests`
- `EngineeringWorkflowModuleBoundaryTests`

## Allowlist policy

- `tests/fixtures/architecture/module-boundary-allowlist.json` is machine-readable and reviewed by tests.
- Every non-empty allowlist entry must include `reason`, `proposedStage`, and `expiresWhen`.
- Vague or permanent exceptions are not allowed.

## Known exceptions

- No active module-boundary exceptions are currently required in `module-boundary-allowlist.json`.
- P8-01 EngineeringWorkflow API-shell deferrals remain tracked in `engineeringworkflow-boundary-allowlist.json` for P8-03 hotspot decomposition.

## Verification

- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\\scripts\\engineering-core\\assert-engineering-core-v1-release-ready.ps1`
- `dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA --evidence .\\artifacts\\ownership-backfill\\dry-run-local --gate-result .\\artifacts\\ownership-backfill\\gate-local --output .\\artifacts\\ownership-backfill\\apply-local --database-provider SQLite --connection-string \"Data Source=fake.db;Password=super-secret\"`
