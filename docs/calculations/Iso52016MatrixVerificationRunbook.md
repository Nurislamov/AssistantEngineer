# ISO 52016 Matrix verification runbook

This runbook provides the single entry point for the ISO 52016 Matrix verification chain.

## Main command

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
```

The command verifies:

1. Matrix solver stage structure and traceability.
2. Low-level Matrix solver baseline fixtures.
3. Application/building-facade Matrix baseline fixtures.
4. Baseline summary exporter.
5. External validation fixtures.
6. External validation anchors.
7. C# guard tests for the verification chain.

## External validation anchors

```powershell
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1
```

This verifies the independent manual engineering anchors:

```text
MANUAL-ISO52016-ANCHOR-001
MANUAL-ISO52016-ANCHOR-002
MANUAL-ISO52016-ANCHOR-003
MANUAL-ISO52016-ANCHOR-004
MANUAL-ISO52016-ANNUAL-8760-001
```

Status: validation anchors only, not complete numerical equivalence.

## Fast checks

Skip all tests and check only files/scripts:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1 -SkipTests
```

Skip only generated summary export:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1 -SkipSummaryExporter
```

Skip application-level baselines:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1 -SkipApplicationBaselines
```

Skip external validation anchors:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1 -SkipExternalValidationAnchors
```

## Generated outputs

The summary exporter writes generated files under:

```text
artifacts/iso52016/matrix-baselines/
```

These files are generated outputs and should not be committed.

## Non-claim

This verification chain is an internal regression and traceability gate. It does not claim exact pyBuildingEnergy, EnergyPlus, ASHRAE 140, or complete ISO 52016 numerical equivalence.

## Engineering edge-case Matrix hardening anchors

The all-in-one Matrix verification chain includes:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1
```

This gate verifies internal engineering hardening anchors for multi-node response, adjacent/unconditioned boundary behavior, timestep energy scaling, sign conventions, and monthly/annual aggregation.

These checks are engineering edge-case hardening only.

Validation anchors only, not complete numerical equivalence.

No pyBuildingEnergy numerical equivalence claim.
No EnergyPlus numerical equivalence claim.
No ASHRAE Standard 140 benchmark-grade claim coverage claim.
No complete ISO 52016 numerical equivalence claim.

## Engineering edge-case hardening stage

`powershell
.\scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1
.\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
`

Engineering edge-case hardening only.

Validation anchors only, not complete numerical equivalence.
## Application integration hardening

The all-in-one verification chain includes:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-application-integration-hardening.ps1
```

This layer verifies application integration hardening only. Validation anchors only, not complete numerical equivalence.

## Application integration hardening

The Matrix all-verification chain includes the Application integration hardening stage gate:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-application-integration-hardening-stage-gate.ps1
```

Application integration hardening only.
Validation anchors only, not complete numerical equivalence.
No pyBuildingEnergy numerical equivalence claim.
No EnergyPlus numerical equivalence claim.
No ASHRAE Standard 140 benchmark-grade claim coverage claim.
No complete ISO 52016 numerical equivalence claim.
## AE-ISO52016-002 Step 01 - physical node model builder

The Matrix all-verification chain also references `scripts/iso52016/verify-iso52016-physical-node-model-stage.ps1`.

This stage adds an ISO52016-inspired physical room/zone node-model builder over the existing Matrix solver. It is an internal engineering anchor stage only. It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

Generated artifacts are not introduced by this step and should not be committed.
## AE-ISO52016-002 Step 02 - physical surface/construction expansion

The Matrix all-verification chain also references `scripts/iso52016/verify-iso52016-physical-surface-model-stage.ps1`.

This stage expands the ISO52016-inspired physical room/zone model builder with explicit surface and construction contracts. It is an internal engineering anchor stage only. It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

Generated artifacts are not introduced by this step and should not be committed.
## AE-ISO52016-002 Step 03 - physical boundary profile stage

The Matrix all-verification chain references `scripts/iso52016/verify-iso52016-physical-boundary-profile-stage.ps1`.

This stage adds per-surface hourly boundary driving temperatures to the ISO52016-inspired physical room/zone model builder. It is an internal engineering anchor stage only. It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

Generated artifacts are not introduced by this step and should not be committed.
## AE-ISO52016-002 Step 04 - physical operation profile stage

The Matrix all-verification chain references `scripts/iso52016/verify-iso52016-physical-operation-profile-stage.ps1`.

This stage adds hourly operation profiles for the ISO52016-inspired physical room/zone model builder and optional Matrix hourly boundary conductance overrides. It is an internal engineering anchor stage only. It is not complete ISO 52016 numerical equivalence, not pyBuildingEnergy numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

Generated artifacts are not introduced by this step and should not be committed.


