# Calculation Module Deepening Inventory

Generated at: 2026-01-01 00:00:00 UTC

## Status

| Field | Value |
|---|---|
| Inventory | Calculation Module Deepening Inventory |
| Version | v1 |
| Status | DeepeningBaseline |
| Source root | src/Backend/AssistantEngineer.Modules.Calculations |
| Tests root | tests/AssistantEngineer.Tests |
| Service files | 117 |
| Contract files | 196 |
| Abstraction files | 48 |
| Calculation tests | 72 |
| Parity tests | 37 |
| Key engines | 12 |
| Missing key engines | 0 |

## Key engines

| Engine | Layer | Exists | Path |
|---|---|---|---|
| RoomLoadCalculationEngine | Room load orchestration | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| LoadAggregationEngine | Aggregation | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| AnnualEnergyBalanceEngine | Annual energy | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| HourlySimulationToAnnualEnergyInputMapper | Annual energy input mapping | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| SystemEnergyEngine | System energy | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| EquipmentSizingEngine | Equipment sizing | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| TransmissionHeatTransferEngine | Envelope transmission | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| VentilationAndInfiltrationLoadEngine | Ventilation | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| InternalGainEngine | Internal gains | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| WindowSolarGainEngine | Window solar gains | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| AnnualWeatherSolarProfileBuilder | Weather/solar profile | True | $(System.Collections.Specialized.OrderedDictionary.path) |
| EnergyCalculationPipelineService | Application pipeline | True | $(System.Collections.Specialized.OrderedDictionary.path) |

## Missing key engines

- none

## Deepening axes

- Input normalization and units policy.
- Scenario fixtures for room, floor, building and annual-energy paths.
- Diagnostics consistency across all calculation engines.
- Cross-engine balance invariants: component sum, aggregation sum, useful/final/primary energy separation.
- Method strategy isolation: simplified, ISO-inspired and future external validation paths must stay explicit.
- No silent fallback: simplifications and adapters must be visible as diagnostics.

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- Does not claim full ISO 13370 implementation.
- Does not claim full EN 15316 system-chain implementation.

## Interpretation

This inventory is a calculation-module deepening baseline.

It does not add new physics by itself.

It defines which calculation engines and guard rails must remain visible before deeper formula changes are made.
