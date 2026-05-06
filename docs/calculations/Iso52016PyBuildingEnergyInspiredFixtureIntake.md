# AE-VALIDATION-PYBE-001 pyBuildingEnergy-inspired fixture intake

## Stage

- Stage id: `AE-VALIDATION-PYBE-001`
- Scope: fixture intake structure and methodology/naming metadata discipline.

## Claim boundary

- Validation/internal engineering anchors only.
- pyBuildingEnergy-inspired methodology alignment lane only.
- No pyBuildingEnergy parity claim.
- No pyBuildingEnergy numerical equivalence claim.
- No copied pyBuildingEnergy code.
- No pyBuildingEnergy runtime dependency.
- No full ISO 52016 parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- ExternalParityCovered is not allowed in this stage.

## Intake rules

1. `sourceKind` must be `PyBuildingEnergyInspiredNaming`.
2. `reference.derivationKind` must be `PyBuildingEnergyInspiredMethodologyNote`.
3. `reference.sourceDescription` must include explicit non-parity wording.
4. `reference` may contain source-note metadata (`name`, `url`, `commit`, notes).
5. Fixtures may carry internal deterministic expected values; they must be described as internal anchors only.

## Source-note rules

- Source-note only.
- Not a parity claim.
- Not external certification.
- No copied code.
- No runtime dependency.

## Future path

Future external numerical comparison can be added in a later stage only after explicit governance, independent evidence, and separate claim-boundary expansion. This stage does not include that step.

## Commands

```powershell
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016PyBuildingEnergyInspired"
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-stage --stage-id AE-VALIDATION-PYBE-001 --skip-tests
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-all --skip-tests
```
