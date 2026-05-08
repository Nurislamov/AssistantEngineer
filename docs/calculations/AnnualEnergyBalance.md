START FILE
# Annual Energy Balance

Annual energy balance is part of the **Energy Calculation equivalence** track.

It converts hourly or representative monthly energy records into:

- monthly heating demand;
- monthly cooling demand;
- annual heating demand;
- annual cooling demand;
- annual total demand;
- energy use intensity;
- peak heating/cooling load;
- component breakdown;
- diagnostics.

## Current calculation paths

The annual energy balance supports two source paths.

### 1. TrueHourlySimulation

This path is used when the application can provide real hourly simulation records.

A true hourly annual result must contain 8760 records for a non-leap-year annual calculation.

When this path is used:

```text
EnergyDataSource = TrueHourlySimulation
IsTrueHourly8760 = true only when HourlyRecordCount == 8760
```

If the record count is not 8760, the result remains usable for the supplied period, but it is not treated as a true annual 8760 simulation.

### 2. MonthlyBalanceAdapter

This path remains as a compatibility fallback.

It converts monthly balances into representative monthly records so that the same annual aggregation engine can be used.

When this path is used:

```text
EnergyDataSource = MonthlyBalanceAdapter
IsTrueHourly8760 = false
```

This path is not a true hourly 8760 simulation.

## Hourly to annual conversion

For each hourly record:

```text
EnergyKWh = LoadW × HourDurationH / 1000
```

Monthly totals are calculated by grouping records by month.

Annual totals are calculated as the sum of monthly totals.

```text
AnnualHeatingKWh = Sum(MonthlyHeatingKWh)
AnnualCoolingKWh = Sum(MonthlyCoolingKWh)
AnnualTotalKWh = AnnualHeatingKWh + AnnualCoolingKWh
```

## Energy use intensity

If building area is available:

```text
EnergyUseIntensityKWhPerM2Year =
    AnnualTotalKWh / BuildingAreaM2
```

If the building area is missing or invalid, the engine returns diagnostics.

## Magnitude component fields

The annual component breakdown includes magnitude fields:

```text
TransmissionKWh
VentilationKWh
MechanicalVentilationKWh
NaturalVentilationKWh
InfiltrationKWh
GroundKWh
SolarGainsKWh
InternalGainsKWh
```

Magnitude fields are non-negative.

They answer the question:

```text
How much component activity occurred over the calculation period?
```

For example:

```text
TransmissionKWh = Sum(max(TransmissionW, 0) × HourDurationH) / 1000
```

These fields are useful for high-level reports and component sizing.

## Signed component balance fields

The annual component breakdown also includes signed/net fields:

```text
NetTransmissionKWh
NetVentilationKWh
NetMechanicalVentilationKWh
NetNaturalVentilationKWh
NetInfiltrationKWh
NetGroundKWh
```

Signed fields preserve the direction of heat flow.

## Sign convention

The sign convention is:

```text
Positive value = heat gain to the room/building
Negative value = heat loss from the room/building
```

Examples:

```text
Outdoor air colder than indoor air:
TransmissionBalanceW < 0
VentilationBalanceW < 0

Outdoor air warmer than indoor air:
TransmissionBalanceW > 0
VentilationBalanceW > 0

Ground colder than room:
GroundBalanceW < 0

Ground warmer than room:
GroundBalanceW > 0
```

Solar and internal gains remain positive gains:

```text
SolarGainsW >= 0
InternalGainsW >= 0
```

Heating and cooling demand fields remain positive demand values:

```text
HeatingLoadW >= 0
CoolingLoadW >= 0
```

## Magnitude vs signed balance

Example for one hour:

```text
Indoor operative temperature = 20°C
Outdoor temperature = -5°C
Envelope UA = 10 W/K
```

Magnitude component:

```text
TransmissionW = 10 × abs(20 - (-5)) = 250 W
```

Signed balance:

```text
TransmissionBalanceW = 10 × (-5 - 20) = -250 W
```

The magnitude says:

```text
250 W of transmission exchange occurred.
```

The signed balance says:

```text
250 W left the room/building through transmission.
```

## Ventilation and infiltration split

The true hourly path exposes outdoor air components separately when the source can evaluate them.

For true hourly records:

- `MechanicalVentilationW` is mechanical ventilation magnitude.
- `NaturalVentilationW` is natural ventilation magnitude.
- `VentilationW` is `MechanicalVentilationW + NaturalVentilationW`.
- `InfiltrationW` is the separate infiltration magnitude.
- `MechanicalVentilationBalanceW` is signed mechanical ventilation balance.
- `NaturalVentilationBalanceW` is signed natural ventilation balance.
- `VentilationBalanceW` is `MechanicalVentilationBalanceW + NaturalVentilationBalanceW`.
- `InfiltrationBalanceW` is the signed infiltration balance.

```text
ComponentBalanceW = ComponentHeatTransferWPerK x (OutdoorTemperatureC - OperativeTemperatureC)
ComponentW = ComponentHeatTransferWPerK x abs(OutdoorTemperatureC - OperativeTemperatureC)
```

Annual mechanical and natural ventilation totals are exposed as:

```text
MechanicalVentilationKWh
NaturalVentilationKWh
NetMechanicalVentilationKWh
NetNaturalVentilationKWh
```

`InfiltrationW = 0` and `InfiltrationBalanceW = 0` can be valid without warning when infiltration assumptions are explicitly zero.

If a source cannot expose infiltration separately and may have included it in ventilation, the mapper reports this with diagnostics:

```text
AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable
```

## Diagnostics

Important diagnostics include:

```text
AnnualEnergy.TrueHourlySimulationUsed
AnnualEnergy.TrueHourlySimulationPartial
AnnualEnergy.MonthlyBalanceAdapter
AnnualEnergy.SourceUnavailable
AnnualEnergy.HourlyComponentBreakdownPartial
AnnualEnergy.SignedComponentBalanceAvailable
AnnualEnergy.VentilationSubcomponentBreakdownAvailable
AnnualEnergy.VentilationSubcomponentBreakdownPartial
AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable
AnnualEnergy.NegativeHourlyValueClamped
SolarWeather.HourlyWeatherSourceUsed
SolarWeather.SyntheticWeatherUsed
```

## Current status

Annual energy balance is currently:

```text
InternalDeterministicTested for existing deterministic fixtures
BenchmarkCompared for active constant hourly deterministic benchmark fixtures and deterministic ventilation split fixture
Application pipeline integrated
TrueHourlySimulation supported when hourly source is available
MonthlyBalanceAdapter fallback documented
Weather source diagnostics documented for hourly source and synthetic monthly-adapter source
Separate hourly infiltration split supported when source data is available
Separate hourly mechanical/natural ventilation split supported when source data is available
Signed component balance supported for transmission, mechanical ventilation, natural ventilation, aggregate ventilation, infiltration and ground
```

It is not marked as:

```text
No ExternalReferenceCovered claim.
```

because the active benchmark fixtures are deterministic benchmark references, not documented external reference evidence.
END FILE
