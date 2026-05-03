# EnergyPlus Validation Fixture Catalog

Generated at: 2026-05-03 06:12:58 UTC

## Status

| Field | Value |
|---|---|
| Catalog | EnergyPlus Validation Fixture Catalog |
| Version | v1 |
| Status | PlannedValidation |
| Registry | docs/validation/EnergyPlusValidationCaseRegistry.json |
| Fixtures root | tests/fixtures/validation/energyplus |
| Reports directory | docs/reports/validation |
| Registry cases | 5 |
| Registry smoke cases | 3 |
| Fixture directories | 3 |
| Fixtures with comparison | 3 |
| Placeholder comparisons | 3 |
| Real EnergyPlus comparisons | 0 |

## Fixtures

| CaseId | Registry listed | Metadata status | Comparison status | Reference status | Metrics | All metrics passed |
|---|---|---|---|---|---:|---|
| EP-SMOKE-001 | True | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 3 | True |
| EP-SMOKE-002 | True | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 4 | True |
| EP-SMOKE-003 | True | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 4 | True |

## Registry cases without fixture

- none

## Fixtures without registry entry

- none

## Fixtures missing required files

- none

## Fixtures missing comparison output

- none

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

## Interpretation

The fixture catalog checks synchronization between the validation registry, committed fixture folders and generated comparison outputs.

Current smoke fixtures are PlaceholderComparison unless a real EnergyPlus reference output is committed.

PlaceholderComparison is not real EnergyPlus validation.

This does not claim exact EnergyPlus numerical parity or ASHRAE 140 validation coverage.
