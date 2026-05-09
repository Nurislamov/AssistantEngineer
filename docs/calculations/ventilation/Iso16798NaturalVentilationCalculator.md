# AE-VENT-001 EN16798-style standard-based natural ventilation calculator

## Stage

- Stage id: `AE-VENT-001`
- Scope: pure C# EN16798-style standard-based natural ventilation calculator with deterministic internal analytical anchors.

## Claim boundary

- EN16798-style standard-based natural ventilation engineering calculator.
- Internal analytical anchors only.
- Not full validation.
- No full EN16798 compliance claim.
- No StandardReference comparison claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.
- No external validation claim.

## Supported scope

- single-sided opening airflow with effective opening area;
- wind-driven component;
- stack-driven component;
- branch selection: wind+stack sum or max(wind, stack);
- opening fraction and opening schedule control;
- indoor/outdoor temperature-difference effect;
- wind speed and opening height effect;
- discharge-coefficient usage;
- optional density correction;
- optional altitude density correction;
- optional occupancy-driven control branch;
- deterministic airflow, ACH, Hve and branch/control diagnostics.

## Formula-level structure

1. Effective opening area:
   - sum of `OpeningAreaM2 * OpeningFraction * ScheduleFraction` for open openings.
2. Effective discharge coefficient:
   - area-weighted coefficient across active openings.
2. Stack component:
   - buoyancy velocity from `g`, useful height, and indoor/outdoor temperature delta.
   - stack airflow uses effective area, discharge coefficient and stack coefficient.
3. Wind component:
   - wind airflow uses wind speed, pressure coefficient, exposure factor, and wind coefficient.
4. Branch selection:
   - `stack + wind` or `max(stack, wind)` by configured branch mode.
5. ACH:
   - `m3/h / room volume`.
   - clamp by `MaximumAirChangesPerHour`.
6. Density:
   - optional temperature and altitude correction of density.
7. Heat transfer coefficient:
   - `rho * cp * clampedAirflowM3PerS` (W/K).

## Diagnostics

- mode (`ClosedOpenings`, `StackOnly`, `WindOnly`, `StackAndWind`);
- selected branch (`WindStackSum`, `MaxWindStack:*`, etc.);
- clamp notices for opening fraction, schedule fraction, opening area, discharge coefficient, and ACH;
- control reason for schedule or occupancy closure;
- summary message with effective area and airflow components.

## Assumptions

- single-zone natural ventilation analytical formulation;
- single-sided opening treatment with configurable coefficients;
- deterministic physical constants and explicit inputs.

## Limitations

- not full EN16798 compliance;
- not full airflow network coupling;
- no moisture/latent coupling;
- no detailed HVAC plant coupling;
- internal analytical anchor model only;
- no external validation claims.

## Fixture list

- `tests/fixtures/ventilation/iso16798-natural/closed-openings-zero-flow.json`
- `tests/fixtures/ventilation/iso16798-natural/stack-only-temperature-delta.json`
- `tests/fixtures/ventilation/iso16798-natural/wind-only-open-window.json`
- `tests/fixtures/ventilation/iso16798-natural/stack-plus-wind-ach-clamped.json`
- `tests/fixtures/ventilation/natural/single-sided-wind-only.json`
- `tests/fixtures/ventilation/natural/single-sided-stack-only.json`
- `tests/fixtures/ventilation/natural/opening-schedule-closed.json`
- `tests/fixtures/ventilation/natural/occupancy-controlled.json`
- `tests/fixtures/ventilation/natural/ach-clamped.json`

## Migration strategy

- existing `NaturalVentilationAirflowService` remains compatibility behavior;
- EN16798-style standard-based calculator is available as enhanced pure engine;
- adapter layer is available for controlled opt-in wiring in a future stage.
