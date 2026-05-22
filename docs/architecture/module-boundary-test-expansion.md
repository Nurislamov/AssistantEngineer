# Module Boundary Test Expansion (P8-02)

## Purpose

Close P8-00 high-risk architecture-test gap by expanding shared module-boundary coverage to include `AssistantEngineer.Modules.EngineeringWorkflow` in the canonical boundary matrix and tests.

## Scope

- Add machine-readable module boundary matrix and schema.
- Expand architecture tests for project-reference and namespace direction checks.
- Add EngineeringWorkflow-specific boundary tests.
- Update audit/map/security docs for P8-02 stage closure.

## Non-claims

- No calculation physics change claim.
- No public API route change claim.
- No DTO shape change claim.
- No ownership backfill execution claim.
- No global EF query filter claim.
- No DB RLS claim.
- No production security certification claim.

## Boundary matrix

- Added `docs/architecture/module-boundary-matrix.md`.
- Added `docs/architecture/module-boundary-matrix.json`.
- Added `docs/architecture/module-boundary-matrix.schema.json`.
- Matrix explicitly includes API, infrastructure, all runtime modules (including EngineeringWorkflow), and tool/test boundaries.

## Tests added/updated

- Added `tests/AssistantEngineer.Tests/Architecture/ModuleBoundaryMatrixTests.cs`.
- Added `tests/AssistantEngineer.Tests/Architecture/EngineeringWorkflowModuleBoundaryTests.cs`.
- Added `tests/AssistantEngineer.Tests/Architecture/P8ModuleBoundaryTestExpansionTests.cs`.
- Updated `tests/AssistantEngineer.Tests/Architecture/ModuleBoundaryTests.cs` to include EngineeringWorkflow and Identity in module-level no-infrastructure checks.

## EngineeringWorkflow coverage

- EngineeringWorkflow is now present in the shared matrix and enforced by module-boundary matrix tests.
- EngineeringWorkflow namespace rules are validated for `Application.*` and (when present) `Domain.*` prefixes.
- EngineeringWorkflow module dependency direction is checked against API and tools boundaries.

## Allowlist policy

- Added machine-readable `tests/fixtures/architecture/module-boundary-allowlist.json`.
- Non-empty allowlist entries require `source`, `target`, `reason`, `proposedStage`, and `expiresWhen`.
- No permanent/vague exceptions are allowed.

## Remaining exceptions

- `module-boundary-allowlist.json` currently has no active exceptions.
- P8-01 API workflow-shell defer entries remain in `engineeringworkflow-boundary-allowlist.json` with P8-03 staging.

## Verification

- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\\scripts\\engineering-core\\assert-engineering-core-v1-release-ready.ps1`
- `dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA --evidence .\\artifacts\\ownership-backfill\\dry-run-local --gate-result .\\artifacts\\ownership-backfill\\gate-local --output .\\artifacts\\ownership-backfill\\apply-local --database-provider SQLite --connection-string \"Data Source=fake.db;Password=super-secret\"`
