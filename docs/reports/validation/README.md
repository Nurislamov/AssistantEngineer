# Engineering Core V1 Validation Reports

## Purpose

This folder contains generated validation readiness and comparison reports.

These reports support the future EnergyPlus / ASHRAE 140-style validation layer.

They do not claim exact EnergyPlus numerical parity and do not claim ASHRAE 140 validation coverage.

## Files

- docs/reports/validation/EP-SMOKE-001-ComparisonReadiness.md
- docs/reports/validation/EP-SMOKE-001-ComparisonResult.json
- docs/reports/validation/EP-SMOKE-001-ComparisonResult.md
- docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.json
- docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.md

## Generation

Generate EP-SMOKE-001 readiness:

    .\scripts\engineering-core\generate-ep-smoke-001-comparison-readiness.ps1

Generate EP-SMOKE-001 placeholder comparison:

    .\scripts\engineering-core\compare-ep-smoke-001-placeholder.ps1

Generate overall validation comparison summary:

    .\scripts\engineering-core\generate-engineering-core-v1-validation-comparison-summary.ps1

Generate all Engineering Core V1 artifacts:

    .\scripts\engineering-core\regenerate-engineering-core-v1-artifacts.ps1

## Current validation status

Current status:

    PlannedValidation

Current comparison status:

    EP-SMOKE-001 = PlaceholderComparison

This means the comparison harness structure is present, but real EnergyPlus reference output has not yet been committed.

## Required non-claims

These reports must keep visible:

- does not claim exact EnergyPlus numerical parity;
- does not claim ASHRAE 140 validation coverage;
- does not claim full ISO 52016 node/matrix solver parity;
- PlaceholderComparison is not real EnergyPlus validation;
- future real validation must remain tolerance-based.

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationComparisonSummaryTests"

## Real fixture intake readiness

Generate EP-SMOKE-001 real fixture readiness report:

    .\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1

Strict future gate:

    .\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture

Generated output:

- docs/reports/validation/EP-SMOKE-001-RealFixtureReadiness.md

## Generic validation fixture runner

Run all committed validation fixtures:

    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1

Strict future mode:

    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1 -RequireRealReferences

Generated outputs:

- docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.json
- docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.md

## Additional smoke fixtures

Generated comparison outputs:

- docs/reports/validation/EP-SMOKE-002-ComparisonResult.json
- docs/reports/validation/EP-SMOKE-002-ComparisonResult.md
- docs/reports/validation/EP-SMOKE-003-ComparisonResult.json
- docs/reports/validation/EP-SMOKE-003-ComparisonResult.md

Current status:

    PlaceholderComparison

These are not real EnergyPlus validation results.

## Fixture catalog

Generate fixture catalog:

    .\scripts\engineering-core\generate-energyplus-validation-fixture-catalog.ps1

Generated outputs:

- docs/validation/EnergyPlusValidationFixtureCatalog.json
- docs/validation/EnergyPlusValidationFixtureCatalog.md
- docs/validation/EnergyPlusValidationFixtureCatalogGuide.md

The catalog synchronizes registry entries, fixture folders and comparison outputs.
