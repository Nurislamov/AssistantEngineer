# Engineering Core V1 Release Notes

## Status

Engineering-core v1 formula gates are closed.

This means the project now has a documented and tested engineering calculation kernel for HVAC heating/cooling loads, weather-driven annual energy integration, domestic hot water, simplified system energy and equipment sizing.

The closed status is based on:

- formula implementation;
- documented units;
- diagnostics coverage;
- deterministic tests;
- 8760 weather gates;
- annual hourly 8760 energy scenario;
- simplified ISO-inspired scope documentation;
- readiness guard tests.

Engineering-core v1 does not claim exact numeric parity with pyBuildingEnergy, EnergyPlus or ASHRAE 140.

## Closed formula gates

The following gates are closed as `ClosedV1` in `FormulaAuditMatrix`.

| Gate | Status |
|---|---|
| HVAC.TRANSMISSION.SIMPLE_UA | ClosedV1 |
| HVAC.VENTILATION.SENSIBLE_AIRFLOW | ClosedV1 |
| HVAC.INTERNAL_GAINS.SENSIBLE | ClosedV1 |
| HVAC.WINDOW_SOLAR.SIMPLE_SHGC | ClosedV1 |
| HVAC.SOLAR.SURFACE_IRRADIANCE_ISOTROPIC | ClosedV1 |
| HVAC.ROOM_LOAD.DESIGN_POINT | ClosedV1 |
| HVAC.AGGREGATION.LOAD_SUMMARY | ClosedV1 |
| HVAC.ANNUAL_ENERGY.HOURLY_KWH | ClosedV1 |
| WEATHER.EPW_8760 | ClosedV1 |
| WEATHER.PVGIS_8760 | ClosedV1 |
| HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC | ClosedV1 |
| HVAC.THERMAL_ZONE.SINGLE_ZONE | ClosedV1 |
| HVAC.GROUND.SIMPLIFIED | ClosedV1 |
| HVAC.ADJACENT_ZONE.SIMPLIFIED | ClosedV1 |
| HVAC.DHW.SIMPLIFIED | ClosedV1 |
| HVAC.SYSTEM_ENERGY.SIMPLIFIED | ClosedV1 |
| HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN | ClosedV1 |

## What ClosedV1 means

`ClosedV1` means:

- the formula or algorithm is implemented;
- the engineering units are documented;
- diagnostics are available;
- invalid mandatory inputs fail the calculation;
- warnings are used for fallbacks and simplifications;
- deterministic tests cover the calculation path;
- known limitations are explicitly documented.

`ClosedV1` does not mean full ISO, EnergyPlus or pyBuildingEnergy numerical parity.

## Validation flow rule

Engineering-core v1 follows this diagnostics rule:

- `Error` means invalid mandatory input and must fail the calculation;
- `Warning` means fallback, simplified assumption or missing optional assumption;
- `Info` means method/source/metadata diagnostics.

A successful calculation result must not contain `CalculationDiagnosticSeverity.Error`.

## Weather and annual energy

The weather-driven annual energy path is closed for v1 through:

- EPW normalized 8760 import gate;
- PVGIS normalized 8760 import gate;
- true hourly 8760 annual energy integration scenario.

Annual energy can be presented as true hourly annual calculation only when:

- `EnergyDataSource = TrueHourlySimulation`;
- `IsTrueHourly8760 = true`;
- `HourlyRecordCount = 8760`.

Monthly adapter, synthetic weather and deterministic short fixtures are allowed for compatibility/tests, but they must not be presented as true hourly 8760 annual simulation.

## Simplified ISO-inspired scope

The following modules are intentionally simplified/inspired, not full standard implementations:

| Module | Correct wording |
|---|---|
| ISO 52016 hourly heat balance | ISO52016-inspired simplified hourly RC / quasi-implicit heat-balance model |
| ISO 13370 ground heat transfer | ISO13370-inspired simplified ground heat-transfer model |
| EN 15316 system energy | EN15316-inspired simplified final/primary energy reporting model |
| EN 12831-3 DHW | EN12831-3-inspired simplified DHW demand model |
| Adjacent zones | Simplified adjacent boundary model, not a coupled multi-zone solver |

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

## Out of scope for v1

The following areas are intentionally out of scope for engineering-core v1:

- latent load;
- moisture balance;
- humidification/dehumidification;
- detailed psychrometrics;
- detailed HVAC plant simulation;
- full coupled multi-zone simulation;
- exact EnergyPlus parity;
- ASHRAE 140 validation coverage.

## Recommended next stage

The next stage after engineering-core v1 is validation and product hardening:

1. Add EnergyPlus / ASHRAE 140-style validation cases.
2. Add documented tolerances for comparative validation.
3. Add public API examples for common HVAC calculation flows.
4. Add frontend-facing diagnostics display.
5. Add report templates that clearly separate assumptions, warnings and calculation results.