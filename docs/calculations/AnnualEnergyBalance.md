START FILE
# Annual Energy Balance

Annual energy balance is part of the **Energy Calculation Parity** track.

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

## Infiltration status

The current true hourly path does not expose infiltration as a separate signed component.

If infiltration is modelled by the current hourly path, it may be included in combined ventilation contribution.

Therefore:

```text
InfiltrationW = 0
InfiltrationBalanceW = 0
```

can mean:

```text
separate infiltration split is not available
```

not necessarily:

```text
there is physically no infiltration
```

The mapper reports this with diagnostics:

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
AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable
AnnualEnergy.NegativeHourlyValueClamped
```

## Current status

Annual energy balance is currently:

```text
InternalDeterministicTested
Application pipeline integrated
TrueHourlySimulation supported when hourly source is available
MonthlyBalanceAdapter fallback documented
Signed component balance supported for available hourly components
```

It is not marked as:

```text
ExternalParityCovered
```

because no external benchmark comparison is currently used as proof.
END FILE