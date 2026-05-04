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