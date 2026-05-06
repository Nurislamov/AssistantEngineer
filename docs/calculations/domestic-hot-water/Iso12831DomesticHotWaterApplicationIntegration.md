# AE-DHW-002 ISO12831-3-inspired DHW application integration

## Stage

- Stage id: `AE-DHW-002`
- Scope: controlled application integration for the ISO12831-3-inspired DHW calculator.

## Claim boundary

- ISO12831-3-inspired domestic hot water application integration.
- Internal deterministic engineering anchors only.
- Compatibility behavior preserved by default.
- No full ISO 12831-3 compliance claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No external certification claim.

## Integration model

1. Existing `DomesticHotWaterDemandService` remains the application entry point.
2. New option flag controls path selection:
   - `UseIso12831InspiredCalculator = false` (default): compatibility behavior.
   - `UseIso12831InspiredCalculator = true`: adapter + pure ISO12831-inspired calculator.
3. Public service contract remains unchanged (`DomesticHotWaterDemandResult`).

## Compatibility guarantee

- Default behavior is preserved.
- Existing response shape and deterministic compatibility semantics remain available.
- Existing hourly profile output remains 8760 when requested.

## Migration path

- Keep compatibility mode as the default in production.
- Enable opt-in mode in controlled environments for side-by-side engineering validation.
- Promote broader opt-in only after downstream verification stages close.

## Limitations

- Internal deterministic engineering anchors only.
- Not a full ISO 12831-3 compliance implementation.
- Not external certification.
- No parity claims with external tools.
