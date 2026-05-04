# ISO 52016 Matrix external validation anchors merge runbook

## Purpose

This runbook closes the ISO 52016 Matrix external validation anchors stage as independent manual engineering validation evidence.

## Non-claims

This stage is explicitly `ValidationAnchorOnly`:

- no pyBuildingEnergy parity claim;
- no EnergyPlus parity claim;
- no ASHRAE 140 validation claim;
- no full ISO 52016 conformance claim;
- no claim that external software outputs are authoritative references.

pyBuildingEnergy-style and EnergyPlus-style naming may appear only as methodological/background naming conventions. The authoritative values in this stage are the manual formulas encoded in the fixture set.

## Required checks before merge

```powershell
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1
.\scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
```

## Optional merge summary

```powershell
.\scripts\iso52016\write-iso52016-matrix-external-validation-anchors-merge-summary.ps1
```

The generated summary files are written to `artifacts/iso52016/external-validation-anchors/` and must remain untracked.

## Commit hygiene

Before merge, confirm that root patch scripts are removed and generated artifacts are not tracked:

```powershell
git status
git ls-files artifacts/iso52016/external-validation-anchors
```

## Explicit non-claims

No pyBuildingEnergy parity is claimed or implied.

This runbook covers validation anchors only, not full parity.

## Non-claims

Validation anchors only, not full parity.

No exact pyBuildingEnergy numerical parity claim.
No exact EnergyPlus numerical parity claim.
No ExternalParityCovered claim.
No FullParityCovered claim.
No pyBuildingEnergy parity.

Generated artifacts under artifacts/iso52016/external-validation-anchors/ must not be committed.

No full ISO 52016 parity claim.

