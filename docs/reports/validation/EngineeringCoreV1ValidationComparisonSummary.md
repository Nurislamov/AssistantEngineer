# Engineering Core V1 Validation Comparison Summary

Generated at: 2026-05-03 05:35:31 UTC

## Status

| Field | Value |
|---|---|
| Summary | Engineering Core V1 Validation Comparison Summary |
| Version | v1 |
| Status | PlannedValidation |
| Registry file | docs/validation/EnergyPlusValidationCaseRegistry.json |
| Total cases | 5 |
| Cases with comparison | 1 |
| Cases passing | 1 |
| Placeholder comparisons | 1 |
| Planned-only cases | 4 |

## Cases

| CaseId | Stage | Registry status | Comparison status | Reference status | Metrics passed | All passed |
|---|---|---|---|---|---:|---|
| ASHRAE140-STYLE-001 | Ashrae140Style | Planned | NotGenerated | NotAvailable | 0/2 | False |
| ASHRAE140-STYLE-002 | Ashrae140Style | Planned | NotGenerated | NotAvailable | 0/2 | False |
| EP-SMOKE-001 | Smoke | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 3/3 | True |
| EP-SMOKE-002 | Smoke | ReferenceFixturePlaceholder | NotGenerated | NotAvailable | 0/3 | False |
| EP-SMOKE-003 | Smoke | ReferenceFixturePlaceholder | NotGenerated | NotAvailable | 0/3 | False |

## Comparison result files

- docs/reports/validation/EP-SMOKE-001-ComparisonResult.json

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

## Interpretation

Validation summary is a readiness and comparison index.

Current EP-SMOKE-001 result is PlaceholderComparison only.

This does not claim exact EnergyPlus numerical parity.

This does not claim ASHRAE 140 validation coverage.

Future real validation must use committed EnergyPlus/reference model files, provenance metadata and documented tolerances.
