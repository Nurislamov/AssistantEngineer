# EnergyPlus Validation Fixture Authoring Guide

## Purpose

This guide explains how to add new EnergyPlus validation fixtures consistently.

The fixture authoring kit supports placeholder comparisons now and real EnergyPlus references later.

## Template folder

Templates live in:

    docs/validation/fixtures/_template

Templates:

- case-metadata.template.json
- assistantengineer-input.template.json
- reference-output.placeholder.template.json
- comparison-tolerances.template.json
- provenance.template.json
- energyplus-output.reference.template.json
- README.template.md

## Scaffold command

Create a new fixture scaffold:

    .\scripts\engineering-core\new-energyplus-validation-fixture.ps1 -CaseId EP-SMOKE-004 -Name "New smoke case"

Optional:

    .\scripts\engineering-core\new-energyplus-validation-fixture.ps1 -CaseId EP-SMOKE-004 -Name "New smoke case" -Purpose "Prepare new smoke validation case" -WeatherSource "Synthetic fixture." -Force

## Required generated fixture files

The scaffold creates:

- tests/fixtures/validation/energyplus/{CASE_ID}/case-metadata.json
- tests/fixtures/validation/energyplus/{CASE_ID}/assistantengineer-input.json
- tests/fixtures/validation/energyplus/{CASE_ID}/reference-output.placeholder.json
- tests/fixtures/validation/energyplus/{CASE_ID}/comparison-tolerances.json
- docs/validation/fixtures/{CASE_ID}/README.md

## Required registry update

After creating a fixture, add it to:

    docs/validation/EnergyPlusValidationCaseRegistry.json

The registry entry must include:

- caseId;
- name;
- stage;
- status;
- source;
- weatherSource;
- geometry;
- envelope;
- internalGains;
- ventilation;
- hvacControl;
- metrics;
- assumptions;
- knownDifferences;
- nonClaims.

## Required local generation

After editing the fixture and registry, run:

    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1
    .\scripts\engineering-core\generate-engineering-core-v1-validation-comparison-summary.ps1
    .\scripts\engineering-core\generate-energyplus-validation-fixture-catalog.ps1

## Future real EnergyPlus reference

When a real EnergyPlus output is ready, add:

- energyplus-model.idf
- weather.epw
- energyplus-output.raw.csv
- energyplus-output.reference.json
- provenance.json

Use templates:

- provenance.template.json
- energyplus-output.reference.template.json

Then run strict mode:

    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1 -RequireRealReferences

## Required non-claims

Every fixture must keep these visible:

- does not claim exact EnergyPlus numerical parity;
- does not claim ASHRAE 140 validation coverage;
- does not claim full ISO 52016 node/matrix solver parity;
- PlaceholderComparison is not real EnergyPlus validation;
- future real validation must remain tolerance-based.

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationFixtureAuthoringKitTests"
