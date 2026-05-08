# AssistantEngineer.Tools.EnergyPlusFixtureAuthoring

C# tool for EnergyPlus validation fixture scaffold authoring.

## Command

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.EnergyPlusFixtureAuthoring\AssistantEngineer.Tools.EnergyPlusFixtureAuthoring.csproj -- new-fixture --case-id EP-SMOKE-004 --name "Fixture name"
```

## PowerShell wrapper

```powershell
.\scripts\engineering-core\new-energyplus-validation-fixture.ps1 -CaseId EP-SMOKE-004 -Name "Fixture name"
```

## Boundary

This tool owns fixture scaffold generation, template expansion and `CaseId` validation.

The PowerShell script is a thin wrapper only.

## Non-claims

New fixtures are placeholder comparisons by default.

PlaceholderComparison is not real EnergyPlus validation.

Future real validation must remain tolerance-based and provenance-backed.

This tool does not claim exact EnergyPlus numerical equivalence.

This tool does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
