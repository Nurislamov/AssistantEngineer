# Natural Ventilation Foundation

## AE-VENT-EN16798-001A purpose

`AE-VENT-EN16798-001A` introduces a deterministic standard-inspired natural ventilation application foundation for opening geometry, pressure estimation, and airflow estimation.

This stage is intended to provide reusable contracts/services and diagnostics, not a full standard implementation.

## Supported flow configurations

- single-sided
- cross ventilation
- wind-only
- stack-only
- combined wind + stack

## Supported opening metadata

- opening area
- opening width and opening height
- opening fraction
- discharge coefficient
- wind pressure coefficient
- opposite wind pressure coefficient
- opening heights (bottom/top/center/reference)
- orientation azimuth

## General engineering equations used

- Wind pressure:
  - `dP_wind = 0.5 * rho * v^2 * dCp`
- Stack pressure:
  - `dP_stack = rho * g * H * abs((T_indoor - T_outdoor) / T_reference_K)`
- Combined pressure:
  - root-sum-square: `dP_combined = sqrt(dP_wind^2 + dP_stack^2)`
- Orifice airflow:
  - `Qv = Cd * A_eff * sqrt(2 * abs(dP) / rho)`

This implementation uses explicit user-provided coefficients and deterministic defaults with diagnostics when values are missing.

## Scope boundaries in this stage

- No full EN16798 compliance claim.
- No full ISO52016 ventilation load coupling.
- No copied normative tables.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.

## Integration note

Existing `Iso16798NaturalVentilationCalculator` and application adapter remain in place.  
This prompt adds canonical ventilation geometry/pressure/airflow foundation contracts and services additively.

## Next prompts

- `AE-VENT-EN16798-001B`: opening controls and schedules.
- `AE-VENT-EN16798-001C`: thermal-zone and hourly load integration.

## AE-VENT-EN16798-001B - Opening controls schedules and operation logic

### Supported control modes

- always closed
- always open
- fixed fraction
- schedule
- occupancy
- temperature
- occupancy and temperature
- night ventilation
- manual

### Supported thresholds and context inputs

- indoor open temperature threshold
- indoor close temperature threshold
- outdoor minimum and maximum temperature thresholds
- indoor-outdoor temperature difference threshold
- occupancy fraction
- schedule fraction
- night-hour flag

### Profile outputs

- opening fraction profile by opening id
- room-level opening fraction profile by room id
- zone-level opening fraction profile by zone id

### Multiple-rule behavior

- when multiple rules apply to the same target/hour, maximum opening fraction is used deterministically

### Scope boundaries in this stage

- No full EN16798 compliance claim.
- No copied normative tables.
- No `pyBuildingEnergy parity` claim.
- No `EnergyPlus parity` claim.
- No `ASHRAE 140 validation` claim.
- No full ISO52016 annual ventilation-load coupling in this prompt.

### Next prompt

- `AE-VENT-EN16798-001C`: thermal-zone and hourly load integration.
