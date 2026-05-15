# MAN-ENG-DHW-001 Derivation

## Purpose

This Tier 1 manual engineering fixture verifies simple domestic hot water useful demand using independent hand arithmetic.
Case name: Simple domestic hot water demand.

## Assumptions

- Fixed daily hot-water volume.
- Water density assumed 1.0 kg/L.
- Fixed cold/hot water temperatures.
- No distribution/storage/circulation losses.
- No system efficiency chain.
- No heat recovery.
- No monthly/annual profile shaping.
- No peak sizing.
- No EN 15316 system chain.
- No ISO 12831-3 detailed method.

## Inputs

- Daily hot water volume = 200.0 L/day
- Water density = 1.0 kg/L
- Water mass = 200.0 kg/day
- Cold water temperature = 10.0 C
- Hot water temperature = 55.0 C
- Delta T = 45.0 K
- Specific heat capacity c = 0.001163 kWh/(kg*K)

## Formula

Q_DHW_kWh_per_day = mass_kg_per_day * c_kWh_per_kgK * deltaT_K

## Units

- Volume: L/day
- Mass: kg/day
- Temperature difference: K
- Energy: kWh/day and Wh/day
- Average power: W

## Step-by-step arithmetic

- mass = 200.0 * 1.0 = 200.0 kg/day
- deltaT = 55.0 - 10.0 = 45.0 K
- Q_DHW_kWh = 200.0 * 0.001163 * 45.0 = 10.467 kWh/day
- Q_DHW_Wh = 10.467 * 1000 = 10467.0 Wh/day
- averagePowerW = 10467.0 / 24.0 = 436.125 W

## Final expected values

- dailyHotWaterVolumeL = 200.0
- waterDensityKgPerL = 1.0
- waterMassKgPerDay = 200.0
- coldWaterTemperatureC = 10.0
- hotWaterTemperatureC = 55.0
- deltaTemperatureK = 45.0
- specificHeatCapacityKWhPerKgK = 0.001163
- dailyUsefulDhwEnergyKWh = 10.467
- dailyUsefulDhwEnergyWh = 10467.0
- averageDailyUsefulDhwPowerW = 436.125

## Explicit exclusions

- distributionLossesExcluded = true
- storageLossesExcluded = true
- circulationLossesExcluded = true
- systemEfficiencyExcluded = true
- heatRecoveryExcluded = true
- monthlyProfileExcluded = true
- annualProfileExcluded = true
- peakSizingExcluded = true
- en15316SystemChainExcluded = true
- iso12831_3DetailedMethodExcluded = true

## Explicit non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No ISO 12831-3 full validation claim.
- No EN 15316 full system-energy validation claim.
