# AE-EN15316-002 EN15316-inspired system energy application integration

## Stage

- Stage id: `AE-EN15316-002`
- Scope: controlled application integration for the EN15316-inspired modular system energy chain.

## Claim boundary

- EN15316-inspired modular system energy application integration.
- Internal deterministic engineering anchors only.
- Compatibility SystemEnergyEngine behavior preserved by default.
- No full EN 15316 compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Integration model

1. Existing `SystemEnergyEngine` remains the application entry point.
2. New option flag controls path selection:
   - `UseEn15316InspiredChain = false` (default): compatibility behavior.
   - `UseEn15316InspiredChain = true`: adapter + pure EN15316-inspired chain calculator.
3. Public service contract remains unchanged (`SystemEnergyResult`).
4. `EnergyCalculationPipelineService` can produce an opt-in useful-energy handoff result for EN15316-style circuit-level processing.

## Mapping strategy

- `SystemEnergyInput` is mapped into end-use modules:
  - heating
  - cooling
  - domestic hot water
  - fan/auxiliary
- emission, distribution, and storage are pass-through in this integration stage.
- generation uses existing efficiency/COP inputs.
- fan energy is mapped once and must not be double-counted.
- pipeline useful-energy handoff preserves:
  - `Standard-Based Calculation` method label;
  - source module metadata;
  - timestep/month traceability;
  - service-type separation (space heating, space cooling, DHW);
  - carrier selection metadata.

## Compatibility guarantee

- Default behavior remains the existing simplified `SystemEnergyEngine` path.
- Existing deterministic system-energy expectations remain valid when opt-in is disabled.
- Opt-in mode enables controlled engineering validation for the modular chain without API shape changes.

## Migration notes

- keep compatibility mode as default in production;
- enable opt-in mode in controlled environments for comparison and diagnostics review;
- evaluate broader rollout only after follow-up integration evidence closes.

## Limitations

- internal integration anchor only;
- not a full EN 15316 compliance implementation;
- not external certification;
- no equivalence claims with external tools.
