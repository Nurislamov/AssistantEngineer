# ISO 52016 Matrix application integration hardening release gate

This gate closes the application integration hardening stage for the ISO 52016 Matrix calculation path.

## Main command

```powershell
.\scripts\iso52016\assert-iso52016-matrix-application-integration-hardening-release-ready.ps1
```

The command verifies:

1. Application integration hardening fixtures and tests exist.
2. The stage gate passes.
3. The merge summary writer can produce generated evidence under `artifacts/iso52016/application-integration-hardening/`.
4. Generated application integration hardening artifacts are not tracked by git.
5. Release documentation and manifests keep claims honest.

## Scope

ApplicationIntegrationHardeningOnly.

Application integration hardening only.
Validation anchors only, not full parity.
No pyBuildingEnergy parity claim.
No EnergyPlus parity claim.
No ASHRAE 140 validation coverage claim.
No full ISO 52016 parity claim.

This release gate does not require generated validation artifacts to be committed.