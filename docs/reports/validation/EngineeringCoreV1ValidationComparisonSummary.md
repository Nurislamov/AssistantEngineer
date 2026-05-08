# Engineering Core V1 Validation Comparison Summary

Generated at: 2026-01-01 00:00:00 UTC

## Status

| Field | Value |
|---|---|
| Status | PlannedValidation |
| Cases with comparison | 3 |
| Cases passing | 3 |
| Placeholder comparisons | 3 |
| Real EnergyPlus comparisons | 0 |
| Planned-only cases | 2 |

## Cases

| CaseId | Stage | Comparison status | Reference status | Metrics | All passed |
|---|---|---|---|---:|---|
| ASHRAE140-STYLE-001 | Ashrae140Style | NotGenerated | NotAvailable | 0/0 | False |
| ASHRAE140-STYLE-002 | Ashrae140Style | NotGenerated | NotAvailable | 0/0 | False |
| EP-SMOKE-001 | Smoke | PlaceholderComparison | PlaceholderReferenceOutput | 3/3 | True |
| EP-SMOKE-002 | Smoke | PlaceholderComparison | PlaceholderReferenceOutput | 4/4 | True |
| EP-SMOKE-003 | Smoke | PlaceholderComparison | PlaceholderReferenceOutput | 4/4 | True |

## Required non-claims

- Does not claim exact EnergyPlus numerical equivalence.
- Does not claim exact StandardReference numerical equivalence.
- Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- Does not claim full ISO 52016 node/matrix solver equivalence.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

Future real validation must use committed EnergyPlus/reference model files.

This does not claim exact EnergyPlus numerical equivalence.

This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
