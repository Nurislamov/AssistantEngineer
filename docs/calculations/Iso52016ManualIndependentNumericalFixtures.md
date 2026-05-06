# AE-VALIDATION-ISO52016-002 manual independent numerical fixtures

## Purpose

`AE-VALIDATION-ISO52016-002` promotes the external-validation fixtures from basic framework anchors to documented manual independent reference cases with explicit arithmetic derivations and comparison diagnostics.

This stage is limited to validation infrastructure and fixture quality. It does not change solver equations, physical model builder behavior, or public application flows.

## Scope boundary

- Validation/internal engineering anchors only.
- Manual independent reference fixtures only.
- Not a parity claim.
- Not external certification.
- No full ISO 52016 parity claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.

## What this stage adds

1. `reference` metadata on each manual independent fixture:
   - derivation document path
   - derivation kind (`ManualIndependentArithmetic`)
   - manual source description
2. Derivation notes for each fixture under:
   - `docs/calculations/validation/iso52016/`
3. Manual independent comparison coverage:
   - test-only calculator for deterministic manual arithmetic
   - comparison engine pass/fail diagnostics by metric and delta

## Fixture set

- `manual-independent-steady-heating-simple-room.json`
- `manual-independent-steady-cooling-simple-room.json`
- `manual-independent-annual-8760-seasonal-loads.json`

## How to add a new manual independent case

1. Add a fixture JSON in `tests/fixtures/iso52016/external-validation/`.
2. Add `reference.derivationDocument`, `reference.derivationKind`, and `reference.sourceDescription`.
3. Add a matching derivation note in `docs/calculations/validation/iso52016/` with assumptions, equations, arithmetic, expected values, and tolerance rationale.
4. Extend the manual independent test calculator and add/adjust tests.
5. Keep non-claims explicit and preserve the validation-only boundary.

## Reading deltas

- The comparison engine reports per-metric:
  - absolute delta
  - relative delta percent
  - tolerance status
  - diagnostics text
- A fixture passes only when all required metrics satisfy absolute and/or relative tolerance policy.

## Verification commands

```powershell
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016ExternalValidation|FullyQualifiedName~Iso52016ManualIndependent"
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-stage --stage-id AE-VALIDATION-ISO52016-002 --skip-tests
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-all --skip-tests
```
