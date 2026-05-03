# Generic EnergyPlus Validation Fixture Comparison Summary

## Status

| Field | Value |
|---|---|
| Runner | GenericEnergyPlusValidationFixtureRunner |
| Status | PlannedValidation |
| Fixtures root | tests/fixtures/validation/energyplus |
| Output directory | docs/reports/validation |
| Fixtures discovered | 1 |
| Comparisons generated | 1 |
| Passing comparisons | 1 |
| Placeholder comparisons | 1 |
| Real EnergyPlus comparisons | 0 |

## Cases

| CaseId | Stage | Comparison status | Reference status | Metrics passed | All passed |
|---|---|---|---|---:|---|
| EP-SMOKE-001 | Smoke | PlaceholderComparison | PlaceholderReferenceOutput | 3/3 | True |

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

## Interpretation

This generic runner compares committed validation fixtures by documented tolerances.

Current placeholder comparisons are not real EnergyPlus validation.

This does not claim exact EnergyPlus numerical parity.

This does not claim ASHRAE 140 validation coverage.
