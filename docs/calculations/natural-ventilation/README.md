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
- No `StandardReference equivalence` claim.
- No `EnergyPlus comparison workflow` claim.
- No `ASHRAE 140 / BESTEST-style validation anchor` claim.

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
- No `StandardReference equivalence` claim.
- No `EnergyPlus comparison workflow` claim.
- No `ASHRAE 140 / BESTEST-style validation anchor` claim.
- No full ISO52016 annual ventilation-load coupling in this prompt.

### Next prompt

- `AE-VENT-EN16798-001C`: thermal-zone and hourly load integration.

## AE-VENT-EN16798-001C - Thermal-zone and hourly load integration

### Room and zone matching

- Openings are matched by `RoomId` first, then `ZoneId`, with untargeted openings treated as global where applicable.
- Hourly environments are evaluated per hour and target (`RoomId`/`ZoneId`) and then converted into hourly airflow calculation inputs.

### Hourly control and airflow chain

- Opening controls are evaluated hourly from configured rules and hourly context.
- Evaluated opening fractions are applied to matching openings for each hour.
- Hourly airflow is then calculated through the deterministic natural ventilation airflow lane.

### Heat-transfer and sensible load preparation

- Ventilation heat transfer coefficient:
  - `H_ve = m_dot_air * cp_air`
- Sensible ventilation load:
  - `Q_ve = H_ve * (T_indoor - T_outdoor)`
- Sign convention:
  - positive sensible load means indoor-to-outdoor heat loss when indoor air is warmer
  - negative sensible load means outdoor-to-indoor heat gain when outdoor air is warmer

### ACH calculation

- `ACH = airflow_m3_h / volume_m3`
- Room ACH uses room volume from topology.
- Zone ACH uses sum of associated room volumes.
- Missing or non-positive volume keeps ACH unavailable and emits diagnostics.

### Zone profile outputs

- zone airflow profile (`m3/h`)
- zone `H_ve` profile (`W/K`)
- zone sensible ventilation load profile (`W`)
- zone ACH profile

### Scope boundaries in this stage

- No full EN16798 compliance claim.
- No copied normative tables.
- No `StandardReference equivalence` claim.
- No `EnergyPlus comparison workflow` claim.
- No `ASHRAE 140 / BESTEST-style validation anchor` claim.
- No full coupled multizone airflow network.
- No forced ISO52016 solver modification in this stage.

### ISO52016 coupling note

- A direct ISO52016 ventilation profile mapper is not forced in this prompt.
- Final ISO52016 ventilation coupling remains future work after targeted contract extension points are selected.

### Next stage

- `AE-DHW-ISO12831-001A`
