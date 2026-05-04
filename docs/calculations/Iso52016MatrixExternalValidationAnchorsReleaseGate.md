# ISO 52016 Matrix external validation anchors release gate

This release gate closes the ISO 52016 Matrix external validation anchors layer as a deterministic validation-anchor package.

Validation anchors only, not full parity.

## What this gate checks

- Simple independent manual validation anchors exist and are verified.
- Annual 8760 manual reference anchor exists and is verified.
- pyBuildingEnergy-style naming anchors exist as naming/traceability anchors only.
- EnergyPlus-style naming anchors exist as naming/traceability anchors only.
- Stage-gate verification is wired into the all-in-one Matrix verification.
- Main Matrix release-ready assertion knows about this external validation anchors release gate.

## Explicit non-claims

- No exact pyBuildingEnergy numerical parity claim.
- No exact EnergyPlus numerical parity claim.
- No ASHRAE 140 validation coverage claim.
- No ExternalParityCovered claim.
- No FullParityCovered claim.
- No full annual building simulation parity claim.

## Generated artifacts

This layer does not require generated validation artifacts to be committed. If future scripts write outputs under `artifacts/`, those outputs must remain ignored unless a later stage explicitly promotes them to committed reference evidence.