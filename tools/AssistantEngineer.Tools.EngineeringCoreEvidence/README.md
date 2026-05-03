# AssistantEngineer.Tools.EngineeringCoreEvidence

C# tool for Engineering Core V1 evidence generation.

## Commands

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreEvidence\AssistantEngineer.Tools.EngineeringCoreEvidence.csproj -- generate-release-evidence
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreEvidence\AssistantEngineer.Tools.EngineeringCoreEvidence.csproj -- generate-export-disclosure-checklist
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreEvidence\AssistantEngineer.Tools.EngineeringCoreEvidence.csproj -- generate-traceability-matrix
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreEvidence\AssistantEngineer.Tools.EngineeringCoreEvidence.csproj -- generate-all-evidence
```

## Boundary

This tool owns release evidence, export disclosure checklist and traceability matrix generation.

The corresponding PowerShell scripts are thin wrappers only.
