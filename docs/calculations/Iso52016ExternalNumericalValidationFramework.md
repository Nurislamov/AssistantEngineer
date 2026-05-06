# AE-VALIDATION-ISO52016-001 - ISO52016 external numerical validation framework

This stage introduces a C#-owned framework for external numerical validation anchors across ISO52016 Matrix and Physical paths.

## Scope

The framework adds typed fixture contracts, a JSON fixture loader, a comparison engine with tolerance policies, diagnostics, and initial manual independent reference fixtures.

This stage does not change solver equations, physical model equations, or runtime calculation behavior.

## Claim boundary

- Validation/internal engineering anchors only.
- Manual independent reference fixtures only.
- No full ISO 52016 parity claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- ExternalParityCovered is not allowed in this stage.

This stage is not a parity claim and not external certification.

## Fixture lanes

- `ManualIndependent`: deterministic manual independent reference fixture lane.
- `PyBuildingEnergyInspiredNaming`: pyBuildingEnergy-inspired methodology alignment lane; not a parity claim.
- `EnergyPlusStyleNaming`: EnergyPlus-style naming lane; not a parity claim.

No pyBuildingEnergy code is copied and no EnergyPlus runtime dependency is added.

## Contracts and services

- Contracts: `Application/Contracts/Validation/Iso52016/*`
- Loader: `Iso52016ExternalValidationFixtureLoader`
- Comparison engine: `Iso52016ExternalValidationComparisonEngine`

The comparison engine calculates absolute deltas and relative deltas, applies absolute/relative tolerance policy, and returns per-metric diagnostics with pass/fail status.

## Step 01 fixtures

Fixtures are located in:

- `tests/fixtures/iso52016/external-validation/manual-independent-steady-heating-simple-room.json`
- `tests/fixtures/iso52016/external-validation/manual-independent-steady-cooling-simple-room.json`
- `tests/fixtures/iso52016/external-validation/manual-independent-annual-8760-seasonal-loads.json`

## Verification commands

Use the existing ISO52016 C# verification owner and thin wrapper entrypoints:

```powershell
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016ExternalValidation"
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-stage --stage-id AE-VALIDATION-ISO52016-001 --skip-tests
.\scripts\iso52016\verify-iso52016-matrix-all.ps1 -SkipTests
```
