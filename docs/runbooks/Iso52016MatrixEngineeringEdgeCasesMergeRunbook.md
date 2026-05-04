# ISO 52016 Matrix engineering edge cases merge runbook

Use this runbook before merging the engineering edge-case hardening stage.

## Required local checks

```powershell
.\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj
```

## Generated merge summary

Optional local evidence can be generated with:

```powershell
.\scripts\iso52016\write-iso52016-matrix-engineering-edge-cases-merge-summary.ps1
```

The output folder is:

```text
artifacts/iso52016/engineering-edge-cases/
```

Generated artifacts must not be committed.

## Merge checklist

- Stage gate is green.
- Main Matrix all-in-one verification is green.
- Full test project is green.
- Git status does not contain generated artifact files.
- Release notes and manifest keep claims honest.

## Explicit non-claims

Engineering edge-case hardening only.

Validation anchors only, not full parity.

No pyBuildingEnergy parity claim.

No EnergyPlus parity claim.

No ASHRAE 140 validation coverage claim.

No full ISO 52016 parity claim.

No ExternalParityCovered claim.

No FullParityCovered claim.