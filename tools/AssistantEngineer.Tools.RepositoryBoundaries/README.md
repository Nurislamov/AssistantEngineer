# AssistantEngineer.Tools.RepositoryBoundaries

C# tool for repository boundary checks.

## Commands

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.RepositoryBoundaries\AssistantEngineer.Tools.RepositoryBoundaries.csproj -- audit-script-boundaries
dotnet run --project .\tools\AssistantEngineer.Tools.RepositoryBoundaries\AssistantEngineer.Tools.RepositoryBoundaries.csproj -- audit-script-boundaries --fail-on-heavy-scripts
```

## Purpose

This tool enforces the project structure:

- `src/Backend` — application code.
- `src/Frontend` — frontend code.
- `tests` — test code.
- `docs` — documentation and generated evidence.
- `tools` — C# automation, validation and release tools.
- `scripts` — thin wrappers only.
- `.github/workflows` — CI entry points that call tools/scripts.
