# ISO 52016 Matrix external validation anchors merge runbook

## Purpose

This runbook closes the ISO 52016 Matrix external validation anchors stage as independent manual engineering validation evidence.

## Non-claims

This stage is explicitly `ValidationAnchorOnly`:

- no StandardReference equivalence claim;
- no EnergyPlus comparison workflow claim;
- no ASHRAE 140 / BESTEST-style validation anchor claim;
- no full ISO 52016 conformance claim;
- no claim that external software outputs are authoritative references.

StandardReference-style and EnergyPlus-style naming may appear only as methodological/background naming conventions. The authoritative values in this stage are the manual formulas encoded in the fixture set.

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

No StandardReference equivalence is claimed or implied.

This runbook covers validation anchors only, not full equivalence claim.

## Non-claims

Validation anchors only, not full equivalence claim.

No exact StandardReference numerical equivalence claim.
No exact EnergyPlus numerical equivalence claim.
No ExternalReferenceCovered claim.
No FullReferenceCovered claim.
No StandardReference equivalence.

Generated artifacts under artifacts/iso52016/external-validation-anchors/ must not be committed.

No full ISO 52016 equivalence claim.

