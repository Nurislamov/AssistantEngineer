# EnergyPlus Real Fixture Intake Policy

## Purpose

This policy defines how real EnergyPlus reference fixtures are added after Engineering Core V1.

Engineering Core V1 is already closed as an engineering formula gate. Real EnergyPlus fixtures are future comparative validation artifacts and must not change the V1 closure claim.

## Scope

This policy applies to:

- EnergyPlus IDF files;
- EnergyPlus weather files;
- EnergyPlus output CSV/JSON files;
- reference output conversions;
- provenance metadata;
- comparison tolerances;
- validation reports;
- future ASHRAE 140-style fixture inputs.

## Required files for a real fixture

A real EnergyPlus fixture should include:

- source EnergyPlus model file;
- weather file or documented synthetic weather equivalent;
- raw EnergyPlus output file;
- normalized reference output JSON;
- provenance metadata;
- comparison tolerances;
- generated comparison result;
- generated comparison report.

For EP-SMOKE-001 the recommended future file names are:

- tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-model.idf
- tests/fixtures/validation/energyplus/EP-SMOKE-001/weather.epw
- tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-output.raw.csv
- tests/fixtures/validation/energyplus/EP-SMOKE-001/energyplus-output.reference.json
- tests/fixtures/validation/energyplus/EP-SMOKE-001/provenance.json

## Provenance requirements

Every real EnergyPlus fixture must document:

- EnergyPlus version;
- operating system;
- source model file name;
- weather file name or synthetic weather definition;
- run date;
- output variables used;
- unit conversions;
- known differences;
- tolerance policy;
- non-claims.

## Comparison requirements

Comparisons must be tolerance-based.

Allowed metric types:

- NumericWithinTolerance;
- DirectionalTrend;
- SameSign.

A passing comparison means:

    AssistantEngineer result is within documented tolerance for that fixture.

It does not mean:

    exact EnergyPlus numerical parity.

## Required non-claims

Every real fixture must keep visible:

- does not claim exact EnergyPlus numerical parity;
- does not claim ASHRAE 140 validation coverage;
- does not claim full ISO 52016 node/matrix solver parity.

## ASHRAE 140 wording rule

Do not write:

    ASHRAE 140 validated.

Allowed wording:

    ASHRAE 140-style comparative case.
    Future ASHRAE 140-style validation case.
    Comparative validation with documented tolerances.

## Intake gate

Use:

    .\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1

Default behavior:

- writes readiness report;
- reports missing real fixture files;
- does not fail while fixture is planned.

Strict future behavior:

    .\scripts\engineering-core\assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture

With -RequireRealFixture, missing real fixture files fail the script.
