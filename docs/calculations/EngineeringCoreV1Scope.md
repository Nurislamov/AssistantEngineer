# Engineering Core V1 Scope

## Goal

AssistantEngineer engineering-core v1 is an engineering calculation kernel for HVAC heating/cooling loads, weather-driven annual energy integration, domestic hot water, simplified system energy and equipment sizing.

The goal is:

- implement practical HVAC engineering formulas;
- use ISO/pyBuildingEnergy as a source of calculation structure and formula principles;
- expose clear diagnostics for assumptions, fallbacks and limitations;
- provide deterministic unit/integration tests;
- support normalized 8760 hourly weather profiles through EPW and PVGIS;
- keep future validation against EnergyPlus / ASHRAE 140 possible.

Engineering-core v1 does not claim exact numeric parity with pyBuildingEnergy, EnergyPlus or ASHRAE 140.

## Closed V1 scope

The following calculation areas are considered closed for engineering-core v1 when their FormulaAuditMatrix status is `ClosedV1`.

| Calculation area | V1 scope |
|---|---|
| Transmission heat transfer | Steady-state component formula `Q = U * A * ΔT * correctionFactor`. |
| Ventilation and infiltration | Sensible-only airflow heat transfer `Q = ρ * cp * Vdot * ΔT`. |
| Internal gains | Sensible people, lighting, equipment, process and custom gains. |
| Window solar gains | Simplified SHGC/shading based window solar gain. |
| Surface irradiance | ISO52010-inspired solar geometry and isotropic sky transposition. |
| Room load | Design-point heating/cooling component aggregation. |
| Load aggregation | Room to thermal zone/floor/building aggregation with optional coincident hourly peak. |
| Weather EPW | Normalized 8760 hourly weather import gate. |
| Weather PVGIS | Normalized 8760 hourly weather import gate. |
| Annual energy | Hourly-to-monthly and hourly-to-annual kWh integration from true hourly 8760 records. |
| ISO52016-inspired hourly heat balance | Simplified hourly RC / quasi-implicit heat-balance model. |
| Single thermal zone | Single-zone engineering path with assigned-room-only aggregation and no double-counting. |
| Ground heat transfer | ISO13370-inspired simplified ground heat-transfer model using equivalent U/H values and boundary weights. |
| Adjacent zones | Simplified adjacent boundary model, not a coupled multi-zone solver. |
| DHW | Simplified DHW demand by water volume, temperature lift and configured losses; compatibility path remains default and ISO12831-3-inspired path is opt-in. |
| System energy | Simplified final/primary energy conversion using efficiency, COP and primary factor. |
| Equipment sizing | Capacity sizing by required load, safety factor and deterministic margin ranking. |

## Simplified / inspired scope

These modules are intentionally described as simplified or inspired models.

| Module | Correct V1 wording |
|---|---|
| ISO 52016 hourly heat balance | ISO52016-inspired simplified hourly RC / quasi-implicit heat-balance model. |
| ISO 13370 ground heat transfer | ISO13370-inspired simplified ground heat-transfer model. |
| EN 15316 system energy | EN15316-inspired simplified final/primary energy reporting model. |
| EN 12831-3 DHW | EN12831-3-inspired simplified DHW demand model with controlled opt-in integration; compatibility path remains default. |
| Adjacent zones | Simplified adjacent boundary model, not a coupled multi-zone solver. |

These modules may use ISO-like concepts, names, DTOs or calculation categories, but they do not claim full standard implementation.

## Explicit non-claims

Engineering-core v1 does not claim:

- full ISO 52016 node/matrix solver parity;
- full ISO 52010 climate conversion parity;
- full ISO 13370 implementation;
- full EN 15316 generation/distribution/storage/emission chain;
- exact pyBuildingEnergy numerical parity;
- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full coupled multi-zone heat-balance simulation;
- detailed HVAC plant simulation;
- latent load calculation;
- moisture balance;
- humidification or dehumidification conditions;
- detailed psychrometric supply-air treatment.

## EnergyPlus / ASHRAE 140 position

EnergyPlus and ASHRAE 140 are future validation layers, not required gates for closing engineering-core v1 formulas.

When added, validation should be treated as comparative engineering validation with documented tolerances, not as exact watt-by-watt parity.

## Weather and annual energy gate

Annual energy can be considered closed for v1 only when the source path supplies true hourly records and the result identifies:

- `EnergyDataSource = TrueHourlySimulation`;
- `IsTrueHourly8760 = true`;
- `HourlyRecordCount = 8760`.

Monthly adapter, synthetic weather and deterministic short fixtures are allowed for tests and diagnostics, but they must not be presented as true annual 8760 simulation.

## Diagnostics rule

Calculation diagnostics must distinguish:

- `Error` — invalid mandatory input; calculation must fail;
- `Warning` — fallback, missing optional assumption, simplified model or partial source;
- `Info` — method/source/assumption metadata.

A successful calculation result must not contain `CalculationDiagnosticSeverity.Error`.

## Naming rule

If a class or DTO uses a standard-like name such as `Iso52016`, `Iso13370`, `EN15316` or `EN12831`, documentation and diagnostics must make the actual scope clear.

Correct examples:

- `ISO52016-inspired simplified hourly heat-balance model`;
- `ISO13370-inspired simplified ground model`;
- `EN15316-inspired simplified system energy model`;
- `EN12831-3-inspired simplified DHW demand model`.

Incorrect examples:

- `full ISO 52016 implementation`;
- `EnergyPlus parity`;
- `ASHRAE 140 covered`;
- `ExternalParityCovered` without documented external fixture, tolerance and passing comparison test.
