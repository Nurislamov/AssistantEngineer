# Repository Boundary Tooling Policy

## Purpose

This policy protects the project from drifting back into large PowerShell automation.

## Required structure

- `src/Backend` — application code.
- `src/Frontend` — frontend code.
- `tests` — test code.
- `docs` — documentation and generated evidence.
- `tools` — C# automation, validation and release tools.
- `scripts` — thin wrappers only.
- `.github/workflows` — CI entry points that call tools/scripts.

## Rule

PowerShell scripts may be used as local and CI entry points.

PowerShell scripts must not own generation, validation or release logic.

C# tools must own automation logic.

## Strict mode

The repository boundary audit supports strict mode:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.RepositoryBoundaries\AssistantEngineer.Tools.RepositoryBoundaries.csproj -- audit-script-boundaries --fail-on-heavy-scripts
```

Strict mode should be enabled only after remaining heavy scripts are migrated to C# tools.
