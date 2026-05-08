# ISO 52016 Matrix external validation anchors release gate

This release gate closes the ISO 52016 Matrix external validation anchors stage.

## Scope

Validation anchors only, not full equivalence claim.

The stage is `ValidationAnchorOnly`. It contains independent manual engineering validation anchors and one annual 8760 constant-weather reference. These anchors are intended to guard basic engineering formulas and solver wiring, not to prove full external-program equivalence.

## Source policy

The authoritative reference type for these fixtures is `IndependentManualEngineeringFormula`.

StandardReference-style and EnergyPlus-style names are allowed as traceability labels only. They do not make either tool an authoritative reference for these anchors.

## Explicit non-claims

- Validation anchors only, not full equivalence claim.
- No exact StandardReference numerical equivalence claim.
- No exact EnergyPlus numerical equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor coverage claim.
- No ExternalReferenceCovered claim.
- No FullReferenceCovered claim.

## Generated artifacts

The release gate may write generated merge evidence under:

```text
artifacts/iso52016/external-validation-anchors/
```

This gate does not require generated validation artifacts to be committed. Generated artifacts must remain ignored and untracked.