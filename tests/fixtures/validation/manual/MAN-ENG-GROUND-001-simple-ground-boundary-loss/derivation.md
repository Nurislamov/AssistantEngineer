# MAN-ENG-GROUND-001 Derivation

## Purpose

This Tier 1 manual engineering fixture verifies simple ground boundary steady heat loss using independent hand arithmetic.
Case name: Simple ground boundary steady heat loss.

## Assumptions

- One ground-contact slab/floor element only.
- Fixed equivalent ground U-value.
- Fixed indoor temperature.
- Fixed effective ground temperature.
- No ISO 13370 perimeter/burial/insulation calculation.
- No detailed ground coupling model.
- No dynamic ground temperature model.
- No monthly ground temperature profile.
- No thermal bridge correction.
- No adjacent room coupling.
- No solar/internal gains.
- No ventilation/infiltration.
- No external wall/window/roof transmission in this case.

## Inputs

- Ground contact area = 50.0 m2
- Equivalent ground U-value = 0.30 W/(m2*K)
- Indoor design temperature = 20.0 C
- Effective ground temperature = 10.0 C
- Delta T = 10.0 K

## Formula

Q_ground_W = A_ground_m2 * U_ground_W_per_m2K * deltaT_K

## Units

- Area: m2
- U-value: W/(m2*K)
- Temperature difference: K
- Heat loss: W

## Step-by-step arithmetic

- deltaT = 20.0 - 10.0 = 10.0 K
- Q_ground = 50.0 * 0.30 * 10.0 = 150.0 W
- Q_ground_kW = 150.0 / 1000 = 0.15 kW

## Final expected values

- groundContactAreaM2 = 50.0
- equivalentGroundUValueWPerM2K = 0.30
- indoorDesignTemperatureC = 20.0
- effectiveGroundTemperatureC = 10.0
- deltaTemperatureK = 10.0
- groundBoundaryHeatLossW = 150.0
- groundBoundaryHeatLossKw = 0.15

## Explicit exclusions

- iso13370PerimeterCalculationExcluded = true
- detailedGroundCouplingExcluded = true
- dynamicGroundTemperatureModelExcluded = true
- monthlyGroundTemperatureProfileExcluded = true
- thermalBridgeExcluded = true
- adjacentRoomCouplingExcluded = true
- solarGainsExcluded = true
- internalGainsExcluded = true
- ventilationExcluded = true
- infiltrationExcluded = true
- externalWallTransmissionExcluded = true
- windowTransmissionExcluded = true
- roofTransmissionExcluded = true

## Explicit non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No ISO 13370 full validation claim.
- No detailed ground coupling validation claim.
