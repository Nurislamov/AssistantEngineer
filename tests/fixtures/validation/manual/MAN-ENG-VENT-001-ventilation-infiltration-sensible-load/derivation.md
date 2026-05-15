# MAN-ENG-VENT-001 Derivation

## Purpose

This Tier 1 manual engineering fixture verifies ventilation and infiltration sensible heating load using independent hand arithmetic.
Case name: Ventilation and infiltration sensible heating load.

## Scope and assumptions

- Mechanical ventilation outdoor air sensible load is included.
- Infiltration sensible load is included.
- No heat recovery effect (efficiency = 0.0).
- No transmission load.
- No solar gains.
- No internal gains.
- No latent load.
- No dynamic thermal-mass effects.
- No ground boundary term.
- No adjacent room coupling.

## Inputs

- Room volume = 150.0 m3
- Indoor design temperature = 20 C
- Outdoor design temperature = -10 C
- Delta T = 30 K
- Mechanical outdoor airflow = 120.0 m3/h
- Infiltration ACH = 0.2 1/h
- Sensible coefficient = 0.33 Wh/(m3*K)

## Formulas

- DeltaT_K = T_indoor_C - T_outdoor_C
- airflow_infiltration_m3_per_h = ACH * Volume_m3
- Q_W = 0.33 * airflow_m3_per_h * DeltaT_K

## Step-by-step arithmetic

Mechanical ventilation:

- Q_mechanical = 0.33 * 120.0 * 30 = 1188.0 W

Infiltration airflow:

- airflow_infiltration = 0.2 * 150.0 = 30.0 m3/h

Infiltration sensible load:

- Q_infiltration = 0.33 * 30.0 * 30 = 297.0 W

Total:

- total_outdoor_airflow = 120.0 + 30.0 = 150.0 m3/h
- Q_total = 1188.0 + 297.0 = 1485.0 W
- Q_total_kW = 1485.0 / 1000 = 1.485 kW

## Expected outputs

- mechanicalVentilationAirflowM3PerH = 120.0
- infiltrationAirflowM3PerH = 30.0
- totalOutdoorAirflowM3PerH = 150.0
- mechanicalVentilationSensibleLoadW = 1188.0
- infiltrationSensibleLoadW = 297.0
- totalVentilationInfiltrationSensibleLoadW = 1485.0
- totalVentilationInfiltrationSensibleLoadKw = 1.485

## Explicit exclusions

- transmissionExcluded = true
- solarGainsExcluded = true
- internalGainsExcluded = true
- latentLoadsExcluded = true
- heatRecoveryExcluded = true
- dynamicEffectsExcluded = true
- groundBoundaryExcluded = true
- adjacentRoomCouplingExcluded = true

## Explicit non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
