# ISO 52016 Matrix release-ready gate

This gate is the pre-merge/pre-release command for the ISO 52016 Matrix calculation path.

## Main command

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
```

The command verifies:

1. Required ISO 52016 Matrix verification scripts and docs exist.
2. Full ISO 52016 Matrix verification chain passes.
3. Full `AssistantEngineer.Tests` test project passes.
4. Generated Matrix baseline summary artifacts are not tracked by git.

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