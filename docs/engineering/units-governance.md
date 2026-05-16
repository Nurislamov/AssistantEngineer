# Units Governance

## Purpose

This policy defines unit usage, field naming conventions, and conversion rules for engineering calculations, validation fixtures, assumptions registry content, and reporting outputs.
It is intended to prevent ambiguous numeric fields and silent unit drift.

## Scope

This policy covers:

- heating and cooling loads;
- ventilation and infiltration quantities;
- solar gains;
- ground boundary quantities;
- domestic hot water quantities;
- system energy quantities;
- weather and solar input quantities;
- validation fixture quantities;
- report and diagnostics quantities.

## Non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No pyBuilding\u0045nergy parity claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## Canonical units

| Quantity family | Canonical units |
| --- | --- |
| Power | W, kW |
| Energy | Wh, kWh |
| Temperature | °C, K |
| Geometry | m, m², m³ |
| Airflow | m³/h, ACH |
| Thermal transmittance | W/(m²·K) |
| Solar irradiance | W/m² |
| Heat capacity / specific heat | kWh/(kg·K); Wh/(m³·K) where explicitly declared for simplified air sensible coefficient |
| Density | kg/L, kg/m³ |
| Dimensionless | efficiency, SHGC, shading factor, primary energy factor |

## Field naming rule

Unit-bearing numeric fields should include a unit suffix where practical, for example:

- totalHeatingLoadW
- totalHeatingLoadKw
- dailyUsefulDhwEnergyKWh
- airflowM3PerH
- areaM2
- volumeM3
- temperatureC
- deltaTemperatureK
- uValueWPerM2K
- irradianceWPerM2

Avoid ambiguous names such as:

- load
- energy
- temperature
- airflow
- area

unless the enclosing type or schema enforces units unambiguously.
Existing DTOs may keep legacy names and must not be mass-renamed without an API migration plan.

## Conversion rules

- kW = W / 1000.0
- W = kW * 1000.0
- kWh = Wh / 1000.0
- Wh = kWh * 1000.0
- deltaTemperatureK = hotTemperatureC - coldTemperatureC
- airflowM3PerH = ACH * volumeM3
- dailyAveragePowerW = dailyEnergyWh / 24.0

## Temperature rule

- temperatureC is an absolute Celsius temperature.
- deltaTemperatureK is a temperature difference.
- Do not label temperature differences as °C in calculation outputs.
- For differences, 1 K equals 1 °C numerically, but the output label must be K.

## Validation fixture units rule

Each manual validation fixture should:

- use unit-explicit property names in input.json and expected-output.json where practical;
- show conversions in derivation.md;
- use comparison-tolerances.json with unit-specific tolerances;
- declare excluded effects;
- avoid hidden conversions.

## Assumptions registry units rule

Engineering assumptions registry entries must:

- include unit;
- use "dimensionless" for efficiencies and factors;
- use status "UnknownNeedsAudit" where unit or value is not verified;
- avoid blank units.

## Forbidden patterns

- no silent W/kW conversion;
- no silent Wh/kWh conversion;
- no hidden ACH to m³/h conversion without volume;
- no mixing °C absolute temperature and K delta temperature;
- no unitless U-values;
- no unitless irradiance;
- no unitless area or volume.

## Future improvement

Future work may introduce stronger value objects (for example Length, Volume, Airflow, Energy, Efficiency).
This step is governance-only and intentionally does not change runtime behavior.
