# ISO52016 Matrix/Solver Seam Risk Register (P9-01B)

## Purpose

Track seam-extraction risks and explicit mitigations before any matrix/solver decomposition implementation.

## Scope

This register covers only design-time extraction risks for internal ISO52016 matrix/solver seams.

## Non-claims

- No calculation physics change claim.
- No expected value change claim.
- No EnergyPlus parity claim.
- No pyBuildingEnergy full parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.

## Risk register

Canonical machine-readable register:
- `docs/validation/iso52016-matrix-solver-seam-risk-register.json`

Key risks covered:
- coefficient sign regression;
- load-vector term omission;
- multi-zone coupling regression;
- aggregation/report mapping drift;
- hidden tolerance broadening;
- accidental validation overclaim.

## Mitigation policy

- No extraction stage can allow behavior change.
- Every seam extraction stage is gated by existing characterization tests and any stage-specific hardening tests.
- Tolerance widening is forbidden unless approved in an explicit calculation-change stage (not part of P9-01B).
- Non-claim boundaries must remain explicit in all design artifacts.
- `mitigationEvidence` is now tracked per risk item and points to concrete characterization/governance tests strengthened in `P9-01B1`.

## Expected-value safety boundary

- `expectedValueChangeAllowed` is `false` for every listed risk item.
- Any proposal that changes expected values is out of scope for P9-01B.

## Next steps

- `P9-01B1` characterization hardening is implemented and linked as mitigation evidence for covered risks.
- Re-check this risk register before starting each extraction stage from `P9-01B2` onward.
