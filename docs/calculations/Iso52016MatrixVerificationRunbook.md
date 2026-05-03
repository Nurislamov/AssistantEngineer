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
5. C# guard tests for the verification chain.

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

## Generated outputs

The summary exporter writes generated files under:

```text
artifacts/iso52016/matrix-baselines/
```

These files are generated outputs and should not be committed.

## Non-claim

This verification chain is an internal regression and traceability gate. It does not claim exact pyBuildingEnergy, EnergyPlus, or ASHRAE 140 parity.