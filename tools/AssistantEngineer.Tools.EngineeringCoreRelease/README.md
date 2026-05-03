# AssistantEngineer.Tools.EngineeringCoreRelease

C# release/profile orchestration tool for Engineering Core V1.

## Commands

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- regenerate-artifacts
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-smoke
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-contracts
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-manifest
dotnet run --project .\tools\AssistantEngineer.Tools.EngineeringCoreRelease\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- assert-release-ready
```

## Boundary

This tool owns release/profile orchestration.

The following scripts are thin wrappers only:

- `scripts/engineering-core/assert-engineering-core-v1-release-ready.ps1`
- `scripts/engineering-core/regenerate-engineering-core-v1-artifacts.ps1`
- `scripts/engineering-core/verify-engineering-core-v1-contracts.ps1`
- `scripts/engineering-core/verify-engineering-core-v1-manifest.ps1`
- `scripts/engineering-core/verify-engineering-core-v1-smoke.ps1`
