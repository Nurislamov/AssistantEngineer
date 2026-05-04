# ISO 52016 Matrix release-ready gate

This gate is the pre-merge/pre-release command for the ISO 52016 Matrix calculation path.

## Main command

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
```

The command verifies:

1. Required ISO 52016 Matrix verification scripts and docs exist.
2. Full ISO 52016 Matrix verification chain passes.
3. Independent manual external validation anchor gate passes through the Matrix verification chain.
4. Full `AssistantEngineer.Tests` test project passes.
5. Generated Matrix baseline summary artifacts are not tracked by git.

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
## External validation anchor non-claim

The release-ready gate includes independent manual ISO 52016 Matrix validation anchors. They are validation anchors only and do not claim pyBuildingEnergy parity, EnergyPlus parity, ASHRAE 140 coverage, or full ISO 52016 validation coverage.

## External validation anchors release-ready coverage

The release-ready gate includes `verify-iso52016-matrix-external-validation-anchors.ps1`.

This confirms the external validation anchor fixture set, docs, manifest, and guard tests are present before the Matrix solver stage is asserted release-ready.

The anchors are validation anchors only and must not be described as full pyBuildingEnergy parity, full EnergyPlus parity, or ASHRAE 140 validation coverage.

## External validation anchor manifest completeness

The release-ready path includes the external validation anchor verification script. That script now guards the expanded fixture set, manifest completeness, unique anchor ids, and explicit non-claims.

This must remain a validation-anchor gate only; it must not be described as full pyBuildingEnergy parity, full EnergyPlus parity, or ASHRAE 140 validation coverage.
