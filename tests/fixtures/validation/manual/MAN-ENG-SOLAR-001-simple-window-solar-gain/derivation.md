# MAN-ENG-SOLAR-001 Derivation

## Purpose

This Tier 1 manual engineering fixture verifies simple window solar heat gain using independent hand arithmetic.
Case name: Simple window solar heat gain.

## Assumptions

- One window only.
- Fixed incident surface irradiance.
- Fixed SHGC.
- Fixed shading factor.
- No solar-position calculation.
- No Perez anisotropic sky model.
- No diffuse/direct split.
- No dynamic glazing behavior.
- No thermal mass response.
- No internal gains.
- No transmission loss.
- No HVAC response.
- No weather-file driven time-series run.

## Inputs

- Window area = 4.0 m2
- Incident surface irradiance = 500.0 W/m2
- SHGC = 0.50
- Shading factor = 0.80

## Formula

Q_solar_W = A_window_m2 * G_surface_W_per_m2 * SHGC * shading_factor

## Units

- Area: m2
- Irradiance: W/m2
- Heat gain: W

## Step-by-step arithmetic

Unshaded gain:

- Q_unshaded = 4.0 * 500.0 * 0.50 = 1000.0 W

Net gain with shading:

- Q_solar = 4.0 * 500.0 * 0.50 * 0.80 = 800.0 W

Shading reduction:

- Q_reduction = 1000.0 - 800.0 = 200.0 W

kW conversion:

- Q_solar_kW = 800.0 / 1000 = 0.8 kW

## Final expected values

- windowAreaM2 = 4.0
- incidentSurfaceIrradianceWPerM2 = 500.0
- solarHeatGainCoefficient = 0.50
- shadingFactor = 0.80
- unshadedSolarGainW = 1000.0
- shadingReductionW = 200.0
- netSolarGainW = 800.0
- netSolarGainKw = 0.8

## Explicit exclusions

- solarPositionCalculationExcluded = true
- perezAnisotropicModelExcluded = true
- diffuseDirectSplitExcluded = true
- dynamicGlazingExcluded = true
- thermalMassExcluded = true
- internalGainsExcluded = true
- transmissionLossExcluded = true
- hvacResponseExcluded = true
- weatherFileExcluded = true

## Explicit non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No Perez anisotropic model validation claim.
- No ISO 52016 full validation claim.
