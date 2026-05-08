# AE-GROUND-002 ISO13370-inspired ground boundary application integration

## Stage

- Stage id: `AE-GROUND-002`
- Scope: controlled application integration for the ISO13370-inspired ground boundary calculator.

## Claim boundary

- ISO13370-inspired ground boundary application integration.
- Internal deterministic engineering anchors only.
- Compatibility behavior preserved by default.
- No full ISO 13370 compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Integration strategy

1. Compatibility mode (default):
   - `UseIso13370InspiredBoundaryCalculator = false`.
   - `Iso13370GroundHeatTransferService` keeps the compatibility formula path.
2. Opt-in mode:
   - `UseIso13370InspiredBoundaryCalculator = true`.
   - service maps room metadata through `Iso13370GroundBoundaryApplicationAdapter`;
   - service uses `Iso13370GroundBoundaryCalculator`;
   - mapped outputs: heat transfer coefficient and boundary weights.
3. Missing metadata fallback:
   - matrix fallback path is preserved in both compatibility and opt-in modes.

## Lifetime safety

- `IGroundHeatTransferService` remains singleton.
- `Iso13370GroundBoundaryCalculator` is singleton.
- `Iso13370GroundTemperatureProfileCalculator` is singleton.
- `Iso13370GroundBoundaryApplicationAdapter` is singleton.
- no scoped service is injected into this singleton ground path.

## Migration notes

- this stage does not switch behavior globally;
- adoption remains explicit via options flag;
- existing room-level and matrix fallback behavior remains stable by default.

## Limitations

- internal integration anchor only;
- not a full ISO 13370 compliance claim;
- not external certification;
- no equivalence claims with external tools.
