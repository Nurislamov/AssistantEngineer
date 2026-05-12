# P3-14 - Energy Calculation Pipeline hotspot refactor

## Status

Implemented.

## Done

- `EnergyCalculationPipelineService` was decomposed into focused partial components.
- Room-load input helpers are isolated in `EnergyCalculationPipelineRoomInputBuilder`.
- Aggregation helpers are isolated in `EnergyCalculationPipelineAggregationExecutor`.
- Preferences loading helpers are isolated in `EnergyCalculationPipelinePreferencesLoader`.
- `EnergyCalculationPipelineService` remains facade/orchestrator.
- Calculation physics are unchanged.
- Public API routes are unchanged.

## Out of scope

- New calculation assumptions.
- New validation standard coverage.
- Broad pipeline rewrite.
- Public API/DTO redesign.
- Performance benchmark campaign.

## Verification

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-p3-14-energy-calculation-pipeline-refactor.ps1
```

