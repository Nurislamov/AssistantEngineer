# MAN-ENG-HEAT-001 Derivation

## Purpose

This Tier 1 manual engineering fixture verifies steady-state single-room heating loss using independent hand arithmetic.
Case name: Steady-state single room heating loss.

## Scope and assumptions

- One room only, no adjacent room coupling.
- No solar gains.
- No internal gains.
- No latent load.
- No dynamic thermal-mass effects.
- No ground boundary term.
- Steady-state design-point calculation (single delta-T condition).

## Inputs

Room geometry:

- Length = 5.0 m
- Width = 4.0 m
- Height = 3.0 m
- Floor area = 20.0 m2
- Volume = 60.0 m3

Design temperatures:

- Indoor design temperature = 20 C
- Outdoor design temperature = -5 C
- Delta T = 25 K

Envelope:

- External wall area = 30.0 m2, U = 0.40 W/(m2*K)
- Window area = 4.0 m2, U = 1.60 W/(m2*K)
- Roof area = 20.0 m2, U = 0.25 W/(m2*K)
- Floor/ground is excluded in this case.

Ventilation:

- Air changes per hour = 0.5 ACH
- Volume = 60.0 m3
- Airflow = 30.0 m3/h
- Sensible formula coefficient = 0.33

## Formulas

Transmission components:

- Q = U * A * DeltaT

Ventilation sensible load:

- airflow_m3_per_h = ACH * Volume_m3
- Q_vent_W = 0.33 * airflow_m3_per_h * DeltaT_K

Total design heating load:

- Q_total_W = Q_transmission_W + Q_ventilation_W

## Step-by-step arithmetic

Transmission:

- Q_wall = 30.0 * 0.40 * 25 = 300.0 W
- Q_window = 4.0 * 1.60 * 25 = 160.0 W
- Q_roof = 20.0 * 0.25 * 25 = 125.0 W
- Q_transmission = 300.0 + 160.0 + 125.0 = 585.0 W

Ventilation:

- airflow = 0.5 * 60.0 = 30.0 m3/h
- Q_ventilation = 0.33 * 30.0 * 25 = 247.5 W

Total:

- Q_total = 585.0 + 247.5 = 832.5 W
- Q_total_kW = 832.5 / 1000 = 0.8325 kW

## Expected outputs

- transmissionHeatLossW = 585.0
- ventilationHeatLossW = 247.5
- totalHeatingLoadW = 832.5
- totalHeatingLoadKw = 0.8325

## Explicit non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
