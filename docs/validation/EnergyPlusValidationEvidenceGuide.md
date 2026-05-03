# Engineering Core V1 Validation Evidence Guide

## Purpose

The validation evidence package is the release-facing summary for the EnergyPlus / ASHRAE 140-style validation layer.

It gathers:

- validation registry;
- validation readiness report;
- fixture catalog;
- generic comparison summary;
- validation comparison summary;
- EP-SMOKE readiness reports;
- real fixture intake readiness;
- validation profile scripts;
- validation CI workflow.

## Generated files

- docs/reports/validation/EngineeringCoreV1ValidationEvidence.json
- docs/reports/validation/EngineeringCoreV1ValidationEvidence.md

## Generation

Run:

    .\scripts\engineering-core\generate-engineering-core-v1-validation-evidence.ps1

Usually run after:

    .\scripts\engineering-core\regenerate-engineering-core-v1-validation-artifacts.ps1

## Current status

Current status:

    PlannedValidation

Current comparison status:

    PlaceholderComparison

Current real EnergyPlus comparison count:

    0

## What this evidence proves

This evidence proves:

- validation registry exists;
- smoke fixtures exist;
- comparison outputs are generated;
- fixture catalog sync is generated;
- validation runner exists;
- validation profile exists;
- validation CI exists;
- fixture authoring kit exists;
- real fixture intake gate exists;
- non-claims remain visible.

## What this evidence does not prove

This evidence does not prove:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity.

## Next milestone

The next real validation milestone is:

    Add first real EnergyPlus model/output for EP-SMOKE-001.

That milestone must include:

- energyplus-model.idf;
- weather.epw or documented synthetic equivalent;
- energyplus-output.raw.csv;
- energyplus-output.reference.json;
- provenance.json;
- updated comparison result;
- tolerance-based report;
- preserved non-claims.

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EnergyPlusValidationEvidencePackageTests"
