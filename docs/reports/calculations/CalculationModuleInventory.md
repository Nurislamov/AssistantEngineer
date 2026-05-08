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
| Calculation tests | 79 |
| equivalence tests | 37 |
| Key engines | 12 |
| Missing key engines | 0 |

## Key engines

| Engine | Layer | Exists | Path |
|---|---|---|---|
| RoomLoadCalculationEngine | Room load orchestration | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/RoomLoads/RoomLoadCalculationEngine.cs` |
| LoadAggregationEngine | Aggregation | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Aggregation/LoadAggregationEngine.cs` |
| AnnualEnergyBalanceEngine | Annual energy | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/AnnualEnergy/AnnualEnergyBalanceEngine.cs` |
| HourlySimulationToAnnualEnergyInputMapper | Annual energy input mapping | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/AnnualEnergy/HourlySimulationToAnnualEnergyInputMapper.cs` |
| SystemEnergyEngine | System energy | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SystemEnergy/SystemEnergyEngine.cs` |
| EquipmentSizingEngine | Equipment sizing | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/EquipmentSizing/EquipmentSizingEngine.cs` |
| TransmissionHeatTransferEngine | Envelope transmission | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Transmission/TransmissionHeatTransferEngine.cs` |
| VentilationAndInfiltrationLoadEngine | Ventilation | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Ventilation/VentilationAndInfiltrationLoadEngine.cs` |
| InternalGainEngine | Internal gains | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/InternalGains/InternalGainEngine.cs` |
| WindowSolarGainEngine | Window solar gains | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/SolarGains/WindowSolarGainEngine.cs` |
| AnnualWeatherSolarProfileBuilder | Weather/solar profile | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/WeatherSolar/AnnualWeatherSolarProfileBuilder.cs` |
| EnergyCalculationPipelineService | Application pipeline | True | `src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Pipeline/EnergyCalculationPipelineService.cs` |

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

- Does not claim exact EnergyPlus numerical equivalence.
- Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- Does not claim full ISO 52016 node/matrix solver equivalence.
- Does not claim full ISO 13370 implementation.
- Does not claim full EN 15316 system-chain implementation.

## Interpretation

This inventory is a calculation-module deepening baseline.

It does not add new physics by itself.

It defines which calculation engines and guard rails must remain visible before deeper formula changes are made.
