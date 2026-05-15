# MAN-ENG-SYS-001 Derivation

## Purpose

This Tier 1 manual engineering fixture verifies a simple useful-to-final system energy chain using independent hand arithmetic.
Case name: Simple useful-to-final system energy chain.

## Assumptions

- Fixed useful thermal demand.
- Fixed distribution efficiency.
- Fixed generation efficiency.
- Fixed auxiliary electricity demand.
- Fixed fuel/electricity primary energy factors.
- No part-load curves.
- No seasonal efficiency correction.
- No storage, control, or emission losses.
- No multiple generators and no renewable fraction split.
- No heat pump COP model.
- No EN 15316 detailed subsystem method.

## Inputs

- usefulThermalDemandKWh = 1000.0
- distributionEfficiency = 0.95
- generationEfficiency = 0.90
- auxiliaryElectricityKWh = 25.0
- fuelPrimaryEnergyFactor = 1.10
- electricityPrimaryEnergyFactor = 2.50

## Formulas

- Q_generator_output = Q_useful / distributionEfficiency
- Q_fuel_final = Q_generator_output / generationEfficiency
- Q_distribution_losses = Q_generator_output - Q_useful
- Q_generation_losses = Q_fuel_final - Q_generator_output
- Q_total_thermal_losses = Q_distribution_losses + Q_generation_losses
- Q_total_final_energy = Q_fuel_final + Q_auxiliary
- Q_fuel_primary = Q_fuel_final * fuelPrimaryEnergyFactor
- Q_aux_primary = Q_auxiliary * electricityPrimaryEnergyFactor
- Q_total_primary = Q_fuel_primary + Q_aux_primary

## Units

- Energy quantities: kWh
- Efficiency quantities: dimensionless
- Primary energy factors: dimensionless multiplier

## Step-by-step arithmetic

- Q_useful = 1000.0 kWh
- Q_generator_output = 1000.0 / 0.95 = 1052.6315789473683 kWh
- Q_fuel_final = 1052.6315789473683 / 0.90 = 1169.5906432748538 kWh
- Q_distribution_losses = 1052.6315789473683 - 1000.0 = 52.631578947368325 kWh
- Q_generation_losses = 1169.5906432748538 - 1052.6315789473683 = 116.95906432748549 kWh
- Q_total_thermal_losses = 52.631578947368325 + 116.95906432748549 = 169.59064327485382 kWh
- Q_total_final_energy = 1169.5906432748538 + 25.0 = 1194.5906432748538 kWh
- Q_fuel_primary = 1169.5906432748538 * 1.10 = 1286.5497076023392 kWh
- Q_aux_primary = 25.0 * 2.50 = 62.5 kWh
- Q_total_primary = 1286.5497076023392 + 62.5 = 1349.0497076023392 kWh

## Final expected values

- usefulThermalDemandKWh = 1000.0
- distributionEfficiency = 0.95
- generationEfficiency = 0.90
- generatorOutputThermalEnergyKWh = 1052.6315789473683
- fuelFinalEnergyKWh = 1169.5906432748538
- distributionLossesKWh = 52.631578947368325
- generationLossesKWh = 116.95906432748549
- totalThermalSystemLossesKWh = 169.59064327485382
- auxiliaryElectricityKWh = 25.0
- totalFinalEnergyKWh = 1194.5906432748538
- fuelPrimaryEnergyFactor = 1.10
- electricityPrimaryEnergyFactor = 2.50
- fuelPrimaryEnergyKWh = 1286.5497076023392
- auxiliaryElectricityPrimaryEnergyKWh = 62.5
- totalPrimaryEnergyKWh = 1349.0497076023392

## Explicit exclusions

- partLoadCurvesExcluded = true
- seasonalEfficiencyExcluded = true
- storageLossesExcluded = true
- controlLossesExcluded = true
- emissionLossesExcluded = true
- multipleGeneratorsExcluded = true
- renewableFractionExcluded = true
- heatPumpCopModelExcluded = true
- en15316DetailedSubsystemMethodExcluded = true
- distributionTemperatureLevelsExcluded = true
- hourlyOperationProfileExcluded = true

## Explicit non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No EN 15316 full validation claim.
- No detailed system-energy validation claim.
