# ISO 52016 Matrix verification gate

The Matrix solver stage has two verification layers:

1. **Structural/stage verification**
   - checks that Matrix source files, docs, traceability files and API guards exist;
   - verifies that the old `Legacy`, `V2Matrix`, `Iso52016V2` and `Iso52016SimulationEngine` paths are absent.

2. **Baseline verification**
   - checks that deterministic Matrix baseline fixtures exist;
   - runs `Iso52016MatrixBaselineFixtureTests`;
   - protects against silent numerical drift.

## Main command

```powershell
.\scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1
```

This command now also calls:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-baselines.ps1 -SkipTests
```

The main command still runs the stage/traceability tests unless `-SkipTests` is supplied.

## Fast structure-only command

```powershell
.\scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1 -SkipTests
```

## Skip baseline file verification

```powershell
.\scripts\iso52016\verify-iso52016-matrix-solver-stage.ps1 -SkipBaselines
```

Use this only when intentionally editing baseline fixture infrastructure.