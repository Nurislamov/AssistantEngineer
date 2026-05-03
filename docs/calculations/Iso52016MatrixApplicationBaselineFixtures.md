# ISO 52016 Matrix application baseline fixtures

These fixtures protect the end-to-end building facade path for ISO 52016 Matrix calculations.

## Scope

The application baseline tests exercise:

```text
Building rooms
→ AnnualClimateData
→ WeatherSolarContext
→ room simulation request builder
→ solar/internal gains
→ ISO 52016 Matrix room simulation
→ building aggregation
```

## Fixture set

| Fixture | Purpose |
| --- | --- |
| `building-cold-two-room-heating.json` | Cold two-room building; heating demand must be positive and cooling must remain zero. |
| `building-hot-single-room-cooling.json` | Hot single-room building with gains and solar; cooling demand must be positive. |

## Why ranges, not exact snapshots

These are application-level regression fixtures. They intentionally assert deterministic ranges and aggregation invariants rather than exact external parity values.

Exact low-level Matrix solver snapshots live in:

```text
tests/AssistantEngineer.Tests/Calculations/Iso52016/Matrix/Baselines/*.json
```

## Non-claim

These fixtures do not claim pyBuildingEnergy, EnergyPlus, or ASHRAE 140 parity.