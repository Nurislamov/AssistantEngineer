# AE-VENT-001 ISO16798-inspired natural ventilation calculator

## Stage

- Stage id: `AE-VENT-001`
- Scope: pure C# natural ventilation calculator with deterministic engineering anchors.

## Claim boundary

- ISO16798-inspired natural ventilation engineering calculator.
- Internal deterministic engineering anchors only.
- No full ISO 16798 compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Calculation structure

1. Effective opening area:
   - sum of `OpeningAreaM2 * OpeningRatio` for open openings.
2. Stack component:
   - buoyancy velocity from `g`, useful height, and indoor/outdoor temperature delta.
   - stack airflow uses discharge coefficient and stack coefficient.
3. Wind component:
   - wind airflow uses wind speed, pressure coefficient, exposure factor, and wind coefficient.
4. Total airflow:
   - `stack + wind` in m3/s and m3/h.
5. ACH:
   - `m3/h / room volume`.
   - clamp by `MaximumAirChangesPerHour`.
6. Heat transfer coefficient:
   - `rho * cp * clampedAirflowM3PerS` (W/K).

## Diagnostics

- mode (`ClosedOpenings`, `StackOnly`, `WindOnly`, `StackAndWind`);
- clamp notices for opening ratio, opening area, discharge coefficient, and ACH;
- summary message with effective area and airflow components.

## Limitations

- internal engineering anchor model, not a full standard-compliance engine;
- no certification output;
- no external-tool equivalence claims.

## Migration strategy

- existing `NaturalVentilationAirflowService` remains compatibility behavior;
- new calculator is added as pure engine;
- adapter layer is available for controlled opt-in wiring in a future stage.
