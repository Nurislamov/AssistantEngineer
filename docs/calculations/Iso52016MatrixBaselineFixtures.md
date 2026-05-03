# ISO 52016 Matrix baseline fixtures

This document records the deterministic baseline fixture set for the ISO 52016 Matrix solver.

## Purpose

The Matrix solver is now the only ISO 52016 calculation path. These fixtures protect against silent numerical drift in the node/matrix hourly solver.

The fixtures are not an external validation claim. They are internal deterministic regression snapshots.

## Fixture set

| Fixture | Purpose |
| --- | --- |
| `neutral-no-hvac.json` | Stable neutral boundary; no heating/cooling demand. |
| `winter-heating-24h.json` | Cold boundary; heating demand and peak heating load snapshot. |
| `summer-cooling-24h.json` | Hot boundary plus gains; cooling demand and peak cooling load snapshot. |
| `mass-lag-heating-1h.json` | Warm mass node with cold boundary; confirms mass node remains separate from air node. |

## Test guard

The guard test is:

```text
tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Iso52016MatrixBaselineFixtureTests.cs
```

It loads each JSON fixture, rebuilds the same Matrix solver request, runs the solver, and compares annual, peak, and representative-hour values with tight numerical tolerance.

## Non-claim

These fixtures do not claim exact pyBuildingEnergy, EnergyPlus, or ASHRAE 140 parity.