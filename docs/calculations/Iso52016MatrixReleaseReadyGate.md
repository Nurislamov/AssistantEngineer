# ISO 52016 Matrix release-ready gate

This gate is the pre-merge/pre-release command for the ISO 52016 Matrix calculation path.

## Main command

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
```

The command verifies:

1. Required ISO 52016 Matrix verification scripts and docs exist.
2. Full ISO 52016 Matrix verification chain passes.
3. External validation anchors are present and guard-tested.
4. Full `AssistantEngineer.Tests` test project passes.
5. Generated Matrix baseline summary artifacts are not tracked by git.

## External validation anchor status

The release gate includes:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
```

Status: validation anchors only, not full parity.

Explicit non-claims:

```text
No pyBuildingEnergy parity claim.
No EnergyPlus parity claim.
No ASHRAE 140 validation coverage claim.
No full ISO 52016 parity claim.
```

## Optional strict mode

Require a clean working tree:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit
```

## Fast local modes

Skip full test project:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -SkipFullTests
```

Skip Matrix verification chain:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -SkipIsoVerification
```

Skip generated artifact check:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -SkipGeneratedArtifactCheck
```

## Generated artifacts

The exporter may create:

```text
artifacts/iso52016/matrix-baselines/summary.json
artifacts/iso52016/matrix-baselines/summary.md
```

These files are generated outputs and must not be committed.

## ISO 52016 Matrix engineering edge-case hardening gate

The release-ready gate includes:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1
```

This is an internal engineering hardening gate. It does not claim pyBuildingEnergy, EnergyPlus, ASHRAE 140, or full ISO 52016 parity.

## Engineering edge-case hardening release-ready gate

The main Matrix release-ready gate includes:

`powershell
.\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
`

Engineering edge-case hardening only.

Validation anchors only, not full parity.
## Application integration hardening

The release-ready Matrix chain includes the application integration hardening verification script.

Status: Application integration hardening only. Validation anchors only, not full parity.

## Application integration hardening release gate

The Matrix release-ready chain includes:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-application-integration-hardening-release-ready.ps1
```

Generated application integration hardening artifacts are written under `artifacts/iso52016/application-integration-hardening/` and must not be committed.