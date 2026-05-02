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
