# EP-SMOKE-001 Fixture

## Purpose

EP-SMOKE-001 is the first EnergyPlus / ASHRAE 140-style validation fixture scaffold.

It prepares the repository structure for a future real EnergyPlus smoke comparison.

## Status

Current status:

    ReferenceFixturePlaceholder

This fixture is not a real EnergyPlus validation result yet.

## Case

Single-zone transmission-only heating smoke case.

Main behavior:

    Q = U * A * ΔT

Fixture values:

- U = 0.35 W/(m²·K)
- A = 180 m²
- Indoor setpoint = 20 °C
- Outdoor temperature = -5 °C
- ΔT = 25 K
- Expected transmission heat loss = 1575 W
- Duration = 24 h
- Expected daily heating energy = 37.8 kWh

## Files

- tests/fixtures/validation/energyplus/EP-SMOKE-001/case-metadata.json
- tests/fixtures/validation/energyplus/EP-SMOKE-001/assistantengineer-input.json
- tests/fixtures/validation/energyplus/EP-SMOKE-001/reference-output.placeholder.json
- tests/fixtures/validation/energyplus/EP-SMOKE-001/comparison-tolerances.json

## Future real EnergyPlus fixture

Future milestone should add:

- EnergyPlus IDF or equivalent source model;
- EPW/weather fixture or documented synthetic weather equivalent;
- real EnergyPlus output CSV/JSON;
- provenance metadata;
- comparison report;
- tolerance-based automated comparison.

## Required non-claims

This fixture does not claim:

- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity.

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusSmoke001FixtureScaffoldTests"

## Placeholder comparison harness

Generate placeholder comparison result:

    .\scripts\engineering-core\compare-ep-smoke-001-placeholder.ps1

Generated outputs:

- docs/reports/validation/EP-SMOKE-001-ComparisonResult.json
- docs/reports/validation/EP-SMOKE-001-ComparisonResult.md

The result status is PlaceholderComparison.

It is not a real EnergyPlus validation and not ASHRAE 140 validation coverage.
