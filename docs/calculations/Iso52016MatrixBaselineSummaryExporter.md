# ISO 52016 Matrix baseline summary exporter

The baseline summary exporter creates a human-readable report from the deterministic Matrix baseline fixtures.

## Command

```powershell
.\scripts\iso52016\export-iso52016-matrix-baseline-summary.ps1
```

Default output:

```text
artifacts/iso52016/matrix-baselines/summary.json
artifacts/iso52016/matrix-baselines/summary.md
```

## Purpose

The exporter does not create or update authoritative baseline values. It only reads the committed JSON fixtures and produces a review summary.

Authoritative files remain:

```text
tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Baselines/*.json
tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixBaselineFixtureTests.cs
```

## Use cases

- quick review of annual heating/cooling snapshots;
- checking peak loads without opening every JSON fixture;
- attaching a readable summary to engineering review notes.

## Non-claim

The summary does not claim external parity. It is an internal regression aid.