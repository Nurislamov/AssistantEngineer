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
