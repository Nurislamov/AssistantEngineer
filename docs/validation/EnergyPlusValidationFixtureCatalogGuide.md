# EnergyPlus Validation Fixture Catalog

## Purpose

The fixture catalog synchronizes:

- docs/validation/EnergyPlusValidationCaseRegistry.json
- tests/fixtures/validation/energyplus/*
- docs/reports/validation/*-ComparisonResult.json
- docs/reports/validation/*-ComparisonResult.md

It helps prevent validation registry entries, fixture folders and generated reports from drifting apart.

## Command

Generate catalog:

    .\scripts\engineering-core\generate-energyplus-validation-fixture-catalog.ps1

Generate all validation outputs first:

    .\scripts\engineering-core\compare-energyplus-validation-fixtures.ps1
    .\scripts\engineering-core\generate-energyplus-validation-fixture-catalog.ps1

## Generated files

- docs/validation/EnergyPlusValidationFixtureCatalog.json
- docs/validation/EnergyPlusValidationFixtureCatalog.md

## Synchronization checks

The catalog tracks:

- registry cases without fixture folders;
- fixture folders without registry entries;
- fixtures missing required files;
- fixtures missing comparison output;
- PlaceholderComparison count;
- RealEnergyPlusComparison count;
- metric count;
- allMetricsPassed status.

## Required fixture files

Every fixture folder should contain:

- case-metadata.json;
- assistantengineer-input.json;
- comparison-tolerances.json;
- reference-output.placeholder.json or energyplus-output.reference.json.

## Required non-claims

The catalog must keep visible:

- does not claim exact EnergyPlus numerical parity;
- does not claim ASHRAE 140 validation coverage;
- does not claim full ISO 52016 node/matrix solver parity;
- PlaceholderComparison is not real EnergyPlus validation;
- future real validation must remain tolerance-based.

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationFixtureCatalogTests"

## Adding new fixtures

Use the fixture authoring guide:

    docs/validation/EnergyPlusValidationFixtureAuthoringGuide.md

Scaffold command:

    .\scripts\engineering-core\new-energyplus-validation-fixture.ps1 -CaseId EP-SMOKE-004 -Name "New smoke case"
