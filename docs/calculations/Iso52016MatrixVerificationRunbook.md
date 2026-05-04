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
5. Independent manual external validation anchors.
6. C# guard tests for the verification chain.

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

This verification chain is an internal regression and traceability gate. It does not claim exact pyBuildingEnergy, EnergyPlus, or ASHRAE 140 parity. External validation anchors are independent manual engineering checks only; they are not full parity evidence.

## External validation anchors layer

The all-in-one verification script now includes `verify-iso52016-matrix-external-validation-anchors.ps1`.

This layer uses independent manual reference anchors only:

- one-hour steady heating/cooling anchors;
- pyBuildingEnergy-style and EnergyPlus-style naming anchors without numerical parity claims;
- a compact annual 8760 manual reference anchor.

These fixtures are source-controlled validation anchors, not generated artifacts.

## External validation anchor Step 02 guard

`verify-iso52016-matrix-external-validation-anchors.ps1` now checks that the anchor fixture set contains at least 10 JSON fixtures, that fixture ids are unique, and that every fixture on disk is listed in `Iso52016MatrixExternalValidationAnchorsManifest.json`.

The guard preserves the same claim boundary: validation anchors only, not full parity.
