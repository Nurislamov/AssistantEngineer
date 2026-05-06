# AE-ISO52016-CONSTRUCTION-002 ISO52016 Construction Envelope Input Integration

## Stage

- Stage id: `AE-ISO52016-CONSTRUCTION-002`
- Scope: controlled integration of construction-layer/mass assemblies into room envelope input through an explicit opt-in flag.

## Claim boundary

- ISO52016-inspired construction/mass envelope input integration.
- Internal deterministic engineering anchors only.
- Compatibility envelope behavior preserved by default.
- Construction/mass path remains opt-in.
- No full ISO 52016 compliance claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No external certification claim.

## Integration summary

- `Iso52016RoomEnvelopeInputCalculator` default behavior remains compatibility-first:
  - transmission uses resolved wall/window U-values;
  - ventilation uses room/default ACH and heat-recovery assumptions;
  - thermal capacity uses current room/default internal-capacity logic.
- Construction/mass integration activates only when:
  - `Iso52016ConstructionOptions.UseConstructionLayerMassInput = true`.

## Construction/mass opt-in path

- construction assemblies are mapped for heat-transfer walls through `Iso52016ConstructionAssemblyApplicationAdapter`;
- explicit domain wall assemblies are used when present;
- otherwise a deterministic compatibility-equivalent fallback assembly is generated from resolved U-value;
- optional equivalent mass layer can be provided for controlled test input scenarios;
- when mapped assemblies are unavailable, calculator falls back to compatibility transmission/capacity behavior.

## Equivalent assembly fallback

- fallback assembly always includes a massless resistance layer to preserve resolved compatibility U-value;
- fallback mass contribution is not forced by default;
- fallback mass can be supplied in controlled opt-in tests where explicit engineering assumptions are needed.

## Limitations

- this stage does not change ISO52016 matrix solver equations;
- this stage does not change ISO52016 physical solver equations;
- this stage does not introduce full ISO52016 envelope compliance behavior;
- this stage does not add new database/domain migrations.

## Future path

- extend adapter mapping to richer domain construction metadata when available;
- keep compatibility behavior as default until additional opt-in validation stages are closed;
- keep disclosure wording explicit for default versus opt-in envelope behavior.
