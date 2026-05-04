# ISO 52016 Matrix external validation anchors merge runbook

Validation anchors only, not full parity.

## Local verification before merge

Run from repository root:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
```

## Optional merge summary

```powershell
.\scripts\iso52016\write-iso52016-matrix-external-validation-anchors-merge-summary.ps1
```

The summary is generated under `artifacts/iso52016/external-validation-anchors/` and must not be committed.

## Claims discipline

Do not add full parity wording unless a later stage adds real parity fixtures and evidence.

Required wording:

- Validation anchors only, not full parity.
- No exact pyBuildingEnergy numerical parity claim.
- No exact EnergyPlus numerical parity claim.
- No ASHRAE 140 validation coverage claim.