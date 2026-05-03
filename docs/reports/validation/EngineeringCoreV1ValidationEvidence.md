# Engineering Core V1 Validation Evidence

Generated at: 2026-01-01 00:00:00 UTC

## Status

| Field | Value |
|---|---|
| Evidence package | Engineering Core V1 Validation Evidence |
| Version | v1 |
| Status | PlannedValidation |
| Registry cases | 5 |
| Registry smoke cases | 3 |
| Fixture catalog cases | 3 |
| Generic runner fixtures discovered | 3 |
| Generic runner comparisons generated | 3 |
| Validation summary cases with comparison | 3 |
| Placeholder comparisons | 3 |
| Real EnergyPlus comparisons | 0 |
| Passing comparisons | 3 |
| Missing evidence files | 0 |

## Interpretation

Validation evidence package proves validation infrastructure readiness, placeholder comparison coverage and fixture synchronization.

It does not claim exact EnergyPlus parity.

It does not claim ASHRAE 140 validation coverage.

## Cases

| CaseId | Stage | Metadata status | Comparison status | Reference status | Metrics | All passed | Has real reference |
|---|---|---|---|---|---:|---|---|
| EP-SMOKE-001 | Smoke | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 3 | True | False |
| EP-SMOKE-002 | Smoke | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 4 | True | False |
| EP-SMOKE-003 | Smoke | ReferenceFixturePlaceholder | PlaceholderComparison | PlaceholderReferenceOutput | 4 | True | False |

## Evidence files

| File | Exists |
|---|---|
| docs/validation/EnergyPlusValidationCaseRegistry.json | True |
| docs/validation/EnergyPlusValidationCaseRegistry.md | True |
| docs/validation/EnergyPlusValidationFixtureCatalog.json | True |
| docs/validation/EnergyPlusValidationFixtureCatalog.md | True |
| docs/validation/EnergyPlusValidationFixtureCatalogGuide.md | True |
| docs/validation/EnergyPlusValidationGenericRunner.md | True |
| docs/validation/EnergyPlusValidationFixtureAuthoringGuide.md | True |
| docs/validation/EnergyPlusRealFixtureIntakePolicy.md | True |
| docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.json | True |
| docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.md | True |
| docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.json | True |
| docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.md | True |
| docs/reports/validation/EP-SMOKE-001-RealFixtureReadiness.md | True |
| docs/reports/EngineeringCoreV1ValidationReadiness.md | True |
| docs/reports/validation/README.md | True |
| scripts/engineering-core/regenerate-engineering-core-v1-validation-artifacts.ps1 | True |
| scripts/engineering-core/verify-engineering-core-v1-validation.ps1 | True |
| .github/workflows/engineering-core-v1-validation.yml | True |

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim exact pyBuildingEnergy numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

## Next milestones

- Add first real EnergyPlus model and output for EP-SMOKE-001.
- Add provenance.json for real EnergyPlus fixture.
- Switch EP-SMOKE-001 from PlaceholderComparison to RealEnergyPlusComparison.
- Keep comparison tolerance-based and non-parity.
- Add additional real fixtures only through fixture authoring kit and intake gate.
