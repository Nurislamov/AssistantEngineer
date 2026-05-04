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

Status: validation anchors only, not full parity.

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

This verification chain is an internal regression and traceability gate. It does not claim exact pyBuildingEnergy, EnergyPlus, ASHRAE 140, or full ISO 52016 parity.

## Engineering edge-case Matrix hardening anchors

The all-in-one Matrix verification chain includes:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases.ps1
```

This gate verifies internal engineering hardening anchors for multi-node response, adjacent/unconditioned boundary behavior, timestep energy scaling, sign conventions, and monthly/annual aggregation.

These checks are engineering edge-case hardening only.

Validation anchors only, not full parity.

No pyBuildingEnergy parity claim.
No EnergyPlus parity claim.
No ASHRAE 140 validation coverage claim.
No full ISO 52016 parity claim.