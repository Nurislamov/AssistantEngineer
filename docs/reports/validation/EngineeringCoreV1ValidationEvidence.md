# Engineering Core V1 Validation Evidence

Generated at: 2026-01-01 00:00:00 UTC

## Status

| Field | Value |
|---|---|
| Evidence | Engineering Core V1 Validation Evidence |
| Status | PlannedValidation |
| Placeholder comparisons | 3 |
| Real EnergyPlus comparisons | 0 |
| Missing evidence files | 0 |

## Cases

| CaseId | Comparison status | Reference status | Metrics | All passed |
|---|---|---|---:|---|
| EP-SMOKE-001 | PlaceholderComparison | PlaceholderReferenceOutput | 3 | True |
| EP-SMOKE-002 | PlaceholderComparison | PlaceholderReferenceOutput | 4 | True |
| EP-SMOKE-003 | PlaceholderComparison | PlaceholderReferenceOutput | 4 | True |

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

This evidence does not claim exact EnergyPlus parity.

This evidence does not claim ASHRAE 140 validation coverage.
