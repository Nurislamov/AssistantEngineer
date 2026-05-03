# AssistantEngineer.Tools.EngineeringCore

This project contains C# automation, validation and release tools for Engineering Core workflows.

PowerShell scripts in `scripts/engineering-core` should stay thin wrappers around this tool.

## Commands

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCore\AssistantEngineer.Tools.EngineeringCore.csproj -- generate-calculation-module-inventory
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCore\AssistantEngineer.Tools.EngineeringCore.csproj -- verify-calculation-module-deepening
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCore\AssistantEngineer.Tools.EngineeringCore.csproj -- verify-calculation-module-balance-invariants
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCore\AssistantEngineer.Tools.EngineeringCore.csproj -- verify-calculation-module-diagnostics-consistency
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCore\AssistantEngineer.Tools.EngineeringCore.csproj -- verify-calculation-module-deepening-all
```

## Boundary

- `src/Backend` remains application code.
- `tests` remains test code.
- `docs` contains documentation and generated evidence.
- `tools` contains C# automation, validation and release tooling.
- `scripts` contains thin wrappers only.
- `.github/workflows` calls tools/scripts.
