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

## Thin wrapper definition

A thin wrapper may:

- accept PowerShell-friendly parameters;
- resolve the repository root;
- translate parameters into command-line arguments;
- call `dotnet run --project .\tools\...`.

A thin wrapper must not:

- parse or construct generated JSON;
- generate Markdown reports;
- scan repository files for inventory logic;
- contain release, validation or calculation business rules;
- create evidence artifacts directly.

## Strict mode

The repository boundary audit supports strict mode:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.RepositoryBoundaries\AssistantEngineer.Tools.RepositoryBoundaries.csproj -- audit-script-boundaries --strict
```

Strict mode fails when any PowerShell script is classified as `HeavyPowerShellLogic` or `UnknownPowerShellScript`.

The legacy flag below is kept as an alias for strict mode:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.RepositoryBoundaries\AssistantEngineer.Tools.RepositoryBoundaries.csproj -- audit-script-boundaries --fail-on-heavy-scripts
```
