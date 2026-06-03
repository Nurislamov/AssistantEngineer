# Validation Evidence Inventory (P9-03 refresh)

## Purpose

Provide a machine-readable and human-readable inventory of validation evidence currently present in the repository, grouped by evidence category, maturity, and provenance tagging.

## Scope

This inventory covers calculation-validation evidence in:

- `docs/validation`;
- `tests/AssistantEngineer.Tests/Calculations`;
- `tests/AssistantEngineer.Tests/Validation`;
- validation-oriented tooling and scripts.

## Evidence categories

- `ManualFixture`
- `IndependentReferenceFixture`
- `InternalInvariant`
- `ReleaseGateManifest`
- `ExternalToolReferenceCandidate`
- `DonorMethodologyReference`
- `HistoricalSmoke`
- `PlannedPlaceholder`
- `UnknownNeedsReview`

## Provenance cross-link

- Canonical model: `docs/validation/validation-fixture-provenance-model.md`
- Canonical inventory: `docs/validation/validation-fixture-provenance-inventory.md`
- ISO52016 component map: `docs/validation/iso52016-component-map.md`
- ISO52016 behavior characterization inventory: `docs/validation/iso52016-behavior-characterization-inventory.md`

## Non-claims

- No calculation physics change claim.
- No pyBuildingEnergy full parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.
- No ownership backfill execution claim.
- No production apply enabled claim.

## Inventory notes

- Entries are repository-evidence pointers only; no generated artifacts are introduced by this stage.
- Evidence category and maturity labels are governance descriptors, not certification labels.
- Planned/placeholder evidence is explicitly marked and not counted as achieved validation evidence.
- Claims remain bounded by `docs/validation/validation-claims-policy.md` and `docs/architecture/terminology-and-claims-vocabulary.md`.
