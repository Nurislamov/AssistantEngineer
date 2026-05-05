# ISO 52016 Matrix application integration hardening merge runbook

## Pre-merge verification

Run:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-application-integration-hardening-release-ready.ps1
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
```

## Generated merge evidence

The merge summary writer can create generated evidence under:

```text
artifacts/iso52016/application-integration-hardening/
```

These generated artifacts must not be committed.

## Scope and non-claims

ApplicationIntegrationHardeningOnly.

Application integration hardening only.
Validation anchors only, not full parity.
No pyBuildingEnergy parity claim.
No EnergyPlus parity claim.
No ASHRAE 140 validation coverage claim.
No full ISO 52016 parity claim.
No ExternalParityCovered claim.
No FullParityCovered claim.