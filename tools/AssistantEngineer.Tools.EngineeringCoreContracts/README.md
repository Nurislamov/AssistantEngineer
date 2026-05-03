# AssistantEngineer.Tools.EngineeringCoreContracts

C# tool for Engineering Core V1 contract snapshot generation.

## Commands

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreContracts\AssistantEngineer.Tools.EngineeringCoreContracts.csproj -- generate-api-contract-snapshots
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreContracts\AssistantEngineer.Tools.EngineeringCoreContracts.csproj -- generate-report-contract-snapshots
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreContracts\AssistantEngineer.Tools.EngineeringCoreContracts.csproj -- generate-all-contract-snapshots
```

## Boundary

This tool owns API and report contract snapshot generation.

The corresponding PowerShell scripts are thin wrappers only.
