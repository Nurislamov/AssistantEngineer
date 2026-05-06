# ISO 52016 Matrix Verification Runbook

This runbook provides the developer entrypoint for the ISO52016 Matrix and Physical-chain verification lane.

Durable orchestration is owned by the C# tool:

```text
tools/AssistantEngineer.Tools.Iso52016Verification
docs/verification/Iso52016VerificationRegistry.json
```

The PowerShell entrypoint is a thin wrapper only:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
```

Equivalent direct command:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-all
```

## Fast Check

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1 -SkipTests
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-all --skip-tests
```

## Stages

List registry stages:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- list-stages
```

Run one stage:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.Iso52016Verification -- verify-stage --stage-id AE-ISO52016-002-STEP-01 --skip-tests
```

Old stage-level scripts may remain as compatibility wrappers, but they are not the source of truth.

## Generated Outputs

Generated artifact paths are listed in `docs/verification/Iso52016VerificationRegistry.json`.

These files are generated outputs and should not be committed, including:

```text
artifacts/iso52016/matrix-baselines/
artifacts/iso52016/external-validation-anchors/
artifacts/iso52016/engineering-edge-cases/
artifacts/iso52016/application-integration-hardening/
```

## Non-Claims

This verification chain is an internal regression and traceability gate.

It does not claim:

- No full ISO 52016 parity claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No complete ISO52010 or ISO52016 compliance claim.

