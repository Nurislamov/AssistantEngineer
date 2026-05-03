# Generic EnergyPlus Validation Fixture Runner

## Purpose

The generic validation fixture runner compares every committed EnergyPlus validation fixture under:

    tests/fixtures/validation/energyplus

A fixture is discovered when it contains:

    comparison-tolerances.json

The runner currently supports EP-SMOKE-001 and is designed to support future EP-SMOKE-002, EP-SMOKE-003 and real EnergyPlus reference outputs.

## Command

Run:

    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1

Strict future mode requiring real EnergyPlus references:

    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1 -RequireRealReferences

## Fixture file convention

Each fixture should include:

- case-metadata.json
- assistantengineer-input.json
- comparison-tolerances.json
- reference-output.placeholder.json or energyplus-output.reference.json

The runner prefers:

    energyplus-output.reference.json

If it does not exist, the runner uses:

    reference-output.placeholder.json

unless -RequireRealReferences is used.

## Generated outputs

Per fixture:

- docs/reports/validation/{CASE-ID}-ComparisonResult.json
- docs/reports/validation/{CASE-ID}-ComparisonResult.md

Global summary:

- docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.json
- docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.md

## Supported metric types

- NumericWithinTolerance
- SameSign
- DirectionalTrend

## Current status

Current EP-SMOKE-001 status:

    PlaceholderComparison

This is not real EnergyPlus validation.

## Required non-claims

The generic runner and generated reports do not claim:

- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity.

Future real validation must remain tolerance-based and must include provenance metadata.
