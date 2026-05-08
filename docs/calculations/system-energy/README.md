# System Energy Calculations

## AE-SYS-EN15316-001A purpose

`AE-SYS-EN15316-001A` creates a deterministic EN15316-style system energy module-chain foundation.

This stage prepares useful energy into pre-generation system loads and generation handoff contracts only.

## Supported end uses

- space heating
- space cooling
- ventilation
- domestic hot water
- auxiliary
- future humidification/dehumidification

## Supported module kinds

- useful demand
- emission
- control
- distribution
- storage
- generation handoff
- auxiliary
- recovery

## Supported deterministic module calculation modes

- disabled
- loss fraction
- fixed efficiency
- fixed loss
- direct profile
- handoff only

## Module-chain behavior

- Module output becomes the next module input.
- End-use useful loads are grouped and summed before module application.
- Generation modules are deferred in this stage and treated as handoff-only.
- Auxiliary electricity is tracked separately from thermal system load.
- Recoverable and non-recoverable losses are tracked separately.

## DHW handoff integration

- `AE-DHW-ISO12831-001B` prepares DHW useful + local-loss system heat requirement.
- `DomesticHotWaterSystemEnergyHandoffAdapter` maps that handoff into `SystemEnergyUsefulLoadSet`.

## Scope boundaries in this stage

- No full EN15316 compliance claim.
- No protected EN15316 tables copied.
- No generator/final energy calculation in this prompt.
- No primary energy calculation in this prompt.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

## Next prompt

`AE-SYS-EN15316-001B` - generator and final energy calculation.

## AE-SYS-EN15316-001B - Generator and final energy calculation

This stage consumes `SystemEnergyGenerationHandoff` from 5A and calculates deterministic final energy by generator, end use, and carrier.

### Supported generator kinds

- boiler
- condensing boiler
- electric resistance
- heat pump
- chiller
- district heating
- district cooling
- biomass/fuel/custom

### Supported generator calculation modes

- disabled
- fixed efficiency
- fixed COP
- fixed EER
- seasonal performance factor
- direct final energy profile
- district handoff
- custom factor
- handoff only

### Supported load split modes

- single generator
- priority order
- fixed fraction
- capacity-limited priority
- custom hourly fraction

### Final energy convention

- Final energy is calculated from supplied system load and performance factor:
  - `finalEnergy = suppliedSystemLoad / performanceFactor`
- Auxiliary electricity is tracked separately and also represented under electricity carrier totals.

### Unmet load convention

- Unmet load is explicitly exposed in results and diagnostics.
- It is not silently discarded.

### Scope boundaries in this stage

- No full EN15316 compliance claim.
- No protected EN15316 tables copied.
- No primary energy calculation in this prompt.
- No renewable/non-renewable primary energy factors in this prompt.
- No CO2 calculation in this prompt.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

## Next prompt

`AE-SYS-EN15316-001C` - primary energy, carrier aggregation and disclosure/reporting.
