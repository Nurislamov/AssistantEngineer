# Engineering Core V1 Validation Comparison Summary

Generated at: 2026-05-03 06:27:23 UTC

## Status

| Field | Value |
|---|---|
| Summary | Engineering Core V1 Validation Comparison Summary |
| Version | v1 |
| Status | PlannedValidation |
| Registry file | docs/validation/EnergyPlusValidationCaseRegistry.json |
| Total cases | 5 |
| Cases with comparison | 3 |
| Cases passing | 3 |
| Placeholder comparisons | 3 |
| Real EnergyPlus comparisons | 0 |
| Planned-only cases | 2 |

## Cases

| CaseId | Stage | Registry status | Comparison status | Reference status | Metrics passed | All passed |
|---|---|---|---|---|---:|---|
| ASHRAE140-STYLE-001 | Ashrae140Style | Planned | NotGenerated | NotAvailable | 0/2 | False |
| ASHRAE140-STYLE-002 | Ashrae140Style | Planned | NotGenerated | NotAvailable | 0/2 | False |
| EP-SMOKE-001 | Smoke | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 3/3 | True |
| EP-SMOKE-002 | Smoke | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 4/4 | True |
| EP-SMOKE-003 | Smoke | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 4/4 | True |

## Comparison result files

- docs/reports/validation/EP-SMOKE-001-ComparisonResult.json
- docs/reports/validation/EP-SMOKE-002-ComparisonResult.json
- docs/reports/validation/EP-SMOKE-003-ComparisonResult.json

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

## Interpretation

Validation summary is a readiness and comparison index.

Current EP-SMOKE results are PlaceholderComparison only.

This does not claim exact EnergyPlus numerical parity.

This does not claim ASHRAE 140 validation coverage.

Future real validation must use committed EnergyPlus/reference model files, provenance metadata and documented tolerances.
