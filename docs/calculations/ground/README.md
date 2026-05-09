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

## Virtual ground lane

- `docs/calculations/Iso13370VirtualGround.md` documents the ISO13370-style virtual ground temperature and slab-on-ground calculation lane.
- This lane is additive and opt-in; default behavior remains unchanged.

## What this prompt intentionally does not do

- No `Full ISO compliance` claim.
- No `Full EN compliance` claim.
- No full ISO13370 transient compliance claim.
- No full ISO52016 dynamic ground coupling claim.
- No external validation claim.
- No `StandardReference equivalence` claim.
- No `EnergyPlus comparison workflow` claim.
- No `ASHRAE 140 / BESTEST-style validation anchor` claim.

## Integration note

An automatic production mapper from thermal-topology ground surfaces to full `GroundBoundaryCalculationInput` is deferred to `AE-GROUND-ISO13370-001B`, because project-specific soil/climate/contact metadata ownership still needs explicit integration boundaries.

## AE-GROUND-ISO13370-001B - Thermal topology and ISO52016 boundary profile integration

### Scope in this stage

- Ground surfaces are identified from thermal topology by `ThermalBoundaryKind.Ground`.
- `GroundSurfaceMetadata` enriches each ground topology surface with:
  - contact kind
  - geometry
  - soil properties
  - climate input
  - source and diagnostics
- `GroundBoundaryTopologyMapper` maps each ground topology surface into `GroundBoundaryCalculationInput`.
- `BuildingGroundBoundaryCalculator` performs batch ground calculations for all mapped ground surfaces in a building topology.
- Ground boundary outputs are produced per surface and aggregated at building level:
  - per-surface equivalent U-value
  - per-surface H (W/K)
  - monthly and hourly ground boundary temperature profiles
  - deterministic diagnostics and disclosure
- `GroundBoundaryTemperatureLookupBuilder` exposes reusable lookup dictionaries for:
  - per-surface hourly temperatures
  - per-surface monthly temperatures
  - per-surface representative ground temperatures
- `ThermalZoneBoundaryGroundTemperatureAdapter` prepares representative ground temperature inputs for current thermal-zone boundary calculations where a single scalar ground temperature is accepted.
- Additive optional mapper:
  - `GroundBoundaryToIso52016BoundaryProfileMapper` maps per-surface 8760 hourly ground temperatures into ISO52016 physical surface hourly boundary-condition contracts without modifying the ISO52016 solver algorithm.

### Intentionally not done in this stage

- No full ISO13370 compliance claim.
- No full ISO52016 dynamic ground coupling claim.
- No external validation anchor claim.
- No `StandardReference equivalence` claim.
- No `EnergyPlus comparison workflow` claim.
- No `ASHRAE 140 / BESTEST-style validation anchor` claim.

### Future work

- Deeper ISO52016 per-surface hourly boundary profile coupling through end-to-end simulation orchestration.
- Additional external validation anchors and comparison harnesses.
