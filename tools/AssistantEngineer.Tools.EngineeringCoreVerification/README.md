# AssistantEngineer.Tools.EngineeringCoreVerification

C# orchestration tool for the Engineering Core V1 verification profile.

PowerShell keeps only a thin wrapper:

```text
scripts/engineering-core/verify-engineering-core-v1.ps1
```

## Usage

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreVerification\AssistantEngineer.Tools.EngineeringCoreVerification.csproj -- --skip-full-dotnet
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreVerification\AssistantEngineer.Tools.EngineeringCoreVerification.csproj -- --fast
```

## Boundary

This tool owns the verification sequence.

Generator scripts called from this tool should themselves be thin wrappers over C# tools.
