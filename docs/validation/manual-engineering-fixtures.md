# Manual Engineering Validation Fixtures

## Purpose

This index tracks Tier 1 manual engineering validation fixtures with independent hand-derivation evidence.

## Active fixtures

### MAN-ENG-HEAT-001

- Name: Steady-state single room heating loss
- Tier: Tier1ManualEngineering
- Domain: HeatingLoad
- Fixture path: `tests/fixtures/validation/manual/MAN-ENG-HEAT-001-steady-state-room-loss/`
- Expected total heating load: 832.5 W (0.8325 kW)
- Status: Active

Non-claims:

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.

### MAN-ENG-VENT-001

- Name: Ventilation and infiltration sensible heating load
- Tier: Tier1ManualEngineering
- Domain: VentilationInfiltration
- Fixture path: `tests/fixtures/validation/manual/MAN-ENG-VENT-001-ventilation-infiltration-sensible-load/`
- Expected total ventilation/infiltration sensible load: 1485.0 W (1.485 kW)
- Status: Active

Non-claims:

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.

### MAN-ENG-SOLAR-001

- Name: Simple window solar heat gain
- Tier: Tier1ManualEngineering
- Domain: SolarGains
- Fixture path: `tests/fixtures/validation/manual/MAN-ENG-SOLAR-001-simple-window-solar-gain/`
- Purpose: validate simple single-window solar gain arithmetic with fixed irradiance, SHGC and shading factor.
- Expected net solar gain: 800.0 W (0.8 kW)
- Status: Active

Explicit exclusions:

- `solarPositionCalculationExcluded = true`
- `perezAnisotropicModelExcluded = true`
- `diffuseDirectSplitExcluded = true`
- `dynamicGlazingExcluded = true`
- `thermalMassExcluded = true`
- `internalGainsExcluded = true`
- `transmissionLossExcluded = true`
- `hvacResponseExcluded = true`
- `weatherFileExcluded = true`

Non-claims:

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No Perez anisotropic model validation claim.
- No ISO 52016 full validation claim.

### MAN-ENG-GROUND-001

- Name: Simple ground boundary steady heat loss
- Tier: Tier1ManualEngineering
- Domain: GroundBoundary
- Fixture path: `tests/fixtures/validation/manual/MAN-ENG-GROUND-001-simple-ground-boundary-loss/`
- Purpose: validate simple ground boundary heat loss arithmetic with fixed equivalent U-value and fixed indoor/ground temperatures.
- Expected ground boundary heat loss: 150.0 W (0.15 kW)
- Status: Active

Explicit exclusions:

- `iso13370PerimeterCalculationExcluded = true`
- `detailedGroundCouplingExcluded = true`
- `dynamicGroundTemperatureModelExcluded = true`
- `monthlyGroundTemperatureProfileExcluded = true`
- `thermalBridgeExcluded = true`
- `adjacentRoomCouplingExcluded = true`
- `solarGainsExcluded = true`
- `internalGainsExcluded = true`
- `ventilationExcluded = true`
- `infiltrationExcluded = true`
- `externalWallTransmissionExcluded = true`
- `windowTransmissionExcluded = true`
- `roofTransmissionExcluded = true`

Non-claims:

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No ISO 13370 full validation claim.
- No detailed ground coupling validation claim.

### MAN-ENG-DHW-001

- Name: Simple domestic hot water demand
- Tier: Tier1ManualEngineering
- Domain: DomesticHotWater
- Fixture path: `tests/fixtures/validation/manual/MAN-ENG-DHW-001-simple-domestic-hot-water-demand/`
- Purpose: validate simple DHW useful-energy arithmetic with fixed volume, density, temperature lift and specific heat capacity.
- Expected daily useful DHW energy: 10.467 kWh/day
- Expected average useful DHW power: 436.125 W
- Status: Active

Explicit exclusions:

- `distributionLossesExcluded = true`
- `storageLossesExcluded = true`
- `circulationLossesExcluded = true`
- `systemEfficiencyExcluded = true`
- `heatRecoveryExcluded = true`
- `monthlyProfileExcluded = true`
- `annualProfileExcluded = true`
- `peakSizingExcluded = true`
- `en15316SystemChainExcluded = true`
- `iso12831_3DetailedMethodExcluded = true`

Non-claims:

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No ISO 12831-3 full validation claim.
- No EN 15316 full system-energy validation claim.

### MAN-ENG-SYS-001

- Name: Simple useful-to-final system energy chain
- Tier: Tier1ManualEngineering
- Domain: SystemEnergy
- Fixture path: `tests/fixtures/validation/manual/MAN-ENG-SYS-001-useful-to-final-energy-chain/`
- Purpose: validate a simple system-energy arithmetic chain from useful demand to final and primary energy.
- Expected fuel final energy: 1169.5906432748538 kWh
- Expected total final energy: 1194.5906432748538 kWh
- Expected total primary energy: 1349.0497076023392 kWh
- Status: Active

Explicit exclusions:

- `partLoadCurvesExcluded = true`
- `seasonalEfficiencyExcluded = true`
- `storageLossesExcluded = true`
- `controlLossesExcluded = true`
- `emissionLossesExcluded = true`
- `multipleGeneratorsExcluded = true`
- `renewableFractionExcluded = true`
- `heatPumpCopModelExcluded = true`
- `en15316DetailedSubsystemMethodExcluded = true`
- `distributionTemperatureLevelsExcluded = true`
- `hourlyOperationProfileExcluded = true`

Non-claims:

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No external-calculator parity claim.
- No full ISO/EN compliance claim.
- No EN 15316 full validation claim.
- No detailed system-energy validation claim.
