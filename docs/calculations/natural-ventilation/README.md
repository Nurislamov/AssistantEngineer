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
  - `ΔP_wind = 0.5 * rho * v² * ΔCp`
- Stack pressure:
  - `ΔP_stack = rho * g * H * abs((T_indoor - T_outdoor) / T_reference_K)`
- Combined pressure:
  - root-sum-square: `ΔP_combined = sqrt(ΔP_wind² + ΔP_stack²)`
- Orifice airflow:
  - `Qv = Cd * A_eff * sqrt(2 * abs(ΔP) / rho)`

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
