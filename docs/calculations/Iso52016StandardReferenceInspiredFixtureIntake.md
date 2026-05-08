# AE-VALIDATION-standard-reference-001 StandardReference-inspired fixture intake

## Stage

- Stage id: `AE-VALIDATION-standard-reference-001`
- Scope: fixture intake structure and methodology/naming metadata discipline.

## Claim boundary

- Validation/internal engineering anchors only.
- StandardReference-inspired methodology alignment lane only.
- No StandardReference equivalence claim.
- No StandardReference numerical equivalence claim.
- No copied StandardReference code.
- No StandardReference runtime dependency.
- No full ISO 52016 equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- ExternalReferenceCovered is not allowed in this stage.

## Intake rules

1. `sourceKind` must be `StandardReferenceInspiredNaming`.
2. `reference.derivationKind` must be `StandardReferenceInspiredMethodologyNote`.
3. `reference.sourceDescription` must include explicit non-equivalence wording.
4. `reference` may contain source-note metadata (`name`, `url`, `commit`, notes).
5. Fixtures may carry internal deterministic expected values; they must be described as internal anchors only.

## Source-note rules

- Source-note only.
- Not a equivalence claim.
- Not external certification.
- No copied code.
- No runtime dependency.

## Future path

Future external numerical comparison can be added in a later stage only after explicit governance, independent evidence, and separate claim-boundary expansion. This stage does not include that step.

## Commands

```powershell
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016StandardReferenceInspired"
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-stage --stage-id AE-VALIDATION-standard-reference-001 --skip-tests
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-all --skip-tests
```

