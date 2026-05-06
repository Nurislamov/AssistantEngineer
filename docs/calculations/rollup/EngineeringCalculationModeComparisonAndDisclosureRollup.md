# AE-CALC-ROLLUP-001 Engineering Calculation Mode Comparison and Disclosure Rollup

## Stage

- Stage id: `AE-CALC-ROLLUP-001`
- Scope: internal rollup for engineering calculation mode cataloging, comparison deltas, and disclosure governance.

## Claim boundary

- Engineering calculation mode comparison and disclosure rollup.
- Internal deterministic engineering governance only.
- Compatibility behavior preserved by default.
- Inspired calculation paths remain opt-in.
- No full ISO/EN compliance claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No external certification claim.

## Why this rollup exists

- make default versus opt-in behavior explicit and auditable;
- align stage closure evidence with active option flags;
- provide deterministic delta interpretation for compatibility versus inspired paths;
- centralize non-claim boundaries and forbidden-claim policy in one governance surface.

## Mode catalog

The rollup catalog tracks, per domain:

- mode id;
- mode kind (`CompatibilityDefault`, `InspiredOptIn`, `ValidationAnchor`, `MethodologyIntake`);
- status (`Default`, `AvailableOptIn`, `InternalAnchorOnly`, `ClosedInternalGate`, `NotImplemented`, `Deprecated`);
- default versus opt-in marker;
- controlling option flag for opt-in modes;
- related stage ids;
- documentation and manifest references;
- claim boundary and forbidden claims.

Current opt-in flags included in the catalog:

- `Iso52016ConstructionOptions.UseConstructionLayerMassInput`
- `NaturalVentilationOptions.UseIso16798InspiredCalculator`
- `Iso13370GroundHeatTransferOptions.UseIso13370InspiredBoundaryCalculator`
- `DomesticHotWaterOptions.UseIso12831InspiredCalculator`
- `SystemEnergyOptions.UseEn15316InspiredChain`

## Comparison metrics and deltas

The comparison engine accepts prepared compatibility and inspired metric sets and computes:

- absolute delta;
- relative delta percent;
- tolerance evaluation (absolute and relative);
- pass/fail/warning status;
- deterministic diagnostic notes.

Zero-baseline compatibility values are handled safely:

- relative delta is reported as null when baseline is zero and inspired is non-zero;
- absolute tolerance remains authoritative for pass/fail in that case.

## Delta interpretation

- `Pass`: at least one configured tolerance guard passes.
- `Warning`: result passes but includes governance warnings (for example zero-baseline relative comparison).
- `Fail`: both tolerance guards fail for one or more metrics.

These deltas are internal governance examples only and do not represent external validation or certification.

## Limitations

- this stage does not run full production orchestration;
- comparison inputs are deterministic prepared metric sets;
- no new physics or solver behavior is introduced;
- no API endpoint or frontend view is introduced in this stage.

## Future path

- optional API exposure for read-only rollup state;
- optional UI panel for default/opt-in status and deterministic deltas;
- optional audit export once disclosure policy stabilizes across modules.
