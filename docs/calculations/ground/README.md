# Ground Boundary Calculation Lane

## AE-GROUND-ISO13370-001A purpose

`AE-GROUND-ISO13370-001A` adds a deterministic internal engineering ground-contact calculation lane inspired by ISO13370 methodology.

This stage provides reusable application-level contracts and services for ground-boundary preparation, diagnostics, and disclosure.

## Supported ground contact kinds

- slab-on-ground
- suspended floor
- heated basement
- unheated basement
- buried wall
- crawlspace
- other

## Required geometry and metadata

- area
- exposed perimeter
- characteristic dimension
- depth below ground
- basement wall height
- crawlspace height
- floor U-value
- wall U-value
- insulation placement
- edge insulation thickness and conductivity (when insulation is declared)

## Ground temperature profile source modes

- hourly outdoor temperatures (8760)
- monthly outdoor temperatures (12)
- annual mean outdoor temperature

## Outputs

- equivalent ground boundary U-value
- heat transfer coefficient (W/K)
- monthly ground boundary temperatures
- hourly ground boundary temperatures (8760)
- diagnostics and standard-inspired disclosure

## What this prompt intentionally does not do

- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No full ISO13370 transient compliance claim.
- No full ISO52016 dynamic ground coupling claim.
- No external validation claim.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

## Integration note

An automatic production mapper from thermal-topology ground surfaces to full `GroundBoundaryCalculationInput` is deferred to `AE-GROUND-ISO13370-001B`, because project-specific soil/climate/contact metadata ownership still needs explicit integration boundaries.
