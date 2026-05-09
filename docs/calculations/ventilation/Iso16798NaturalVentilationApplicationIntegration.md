# AE-VENT-002 ISO16798-inspired natural ventilation application integration

## Stage

- Stage id: `AE-VENT-002`
- Scope: controlled application integration for the ISO16798-inspired natural ventilation calculator.

## Claim boundary

- ISO16798-inspired natural ventilation application integration.
- Internal deterministic engineering anchors only.
- Compatibility behavior preserved by default.
- No full ISO 16798 compliance claim.
- No StandardReference comparison claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Integration model

1. Existing `NaturalVentilationAirflowService` remains the application entry point.
2. New option flag controls path selection:
   - `UseIso16798InspiredCalculator = false` (default): compatibility behavior.
   - `UseIso16798InspiredCalculator = true`: adapter + pure ISO16798-inspired calculator.
3. Public service contract remains unchanged (`double` heat transfer coefficient).

## Compatibility guarantee

- Default behavior is preserved.
- Existing production semantics for disabled natural ventilation, missing parameters, and closed openings remain unchanged.

## Migration path

- Keep default mode during rollout.
- Enable flag in controlled environments for comparison and diagnostics review.
- Promote opt-in usage only after engineering acceptance in downstream stages.

## Limitations

- Internal engineering anchor integration only.
- Not a full standards-compliance engine.
- Not external certification.
