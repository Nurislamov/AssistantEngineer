# Validation Fixture Provenance Inventory (P9-03)

## Purpose

Provide normalized provenance metadata for validation fixtures/manifests/registries and separate achieved evidence from planned placeholders.

## Scope

The inventory covers repository evidence referenced by validation docs, tests, scripts, and tools for:

- ISO52010 solar-weather chain
- ISO52016 matrix and annual anchors
- heating/cooling manual anchors
- workflow/report behavior anchors
- external comparison registries and fixture catalogs

## Non-claims

- No calculation physics change claim.
- No expected numerical values change claim.
- No pyBuildingEnergy full parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- No ISO certification claim.
- No fully validated claim.

## Category policy

- `PlannedPlaceholder` entries are planning artifacts only.
- `PlannedPlaceholder` entries are not achieved validation evidence.
- `UnknownNeedsReview` entries are explicitly weak and require follow-up stage tagging.

## Expected-value policy

P9-03 does not alter fixture expected numerical values. Every inventory entry is tagged with `expectedValuesChangedInP903=false`.

## Cross-links

- Canonical model: `docs/validation/validation-fixture-provenance-model.md`
- Evidence inventory: `docs/validation/validation-evidence-inventory.md`
- ISO52016 component map: `docs/validation/iso52016-component-map.md`
