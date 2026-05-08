# {{CASE_ID}} Fixture

## Purpose

{{CASE_ID}} is an EnergyPlus validation fixture scaffold.

## Status

Current status:

    ReferenceFixturePlaceholder

Current comparison status after generic runner:

    PlaceholderComparison

This fixture is not a real EnergyPlus validation result yet.

## Files

- tests/fixtures/validation/energyplus/{{CASE_ID}}/case-metadata.json
- tests/fixtures/validation/energyplus/{{CASE_ID}}/assistantengineer-input.json
- tests/fixtures/validation/energyplus/{{CASE_ID}}/reference-output.placeholder.json
- tests/fixtures/validation/energyplus/{{CASE_ID}}/comparison-tolerances.json

## Future real EnergyPlus files

Future real fixture may add:

- tests/fixtures/validation/energyplus/{{CASE_ID}}/energyplus-model.idf
- tests/fixtures/validation/energyplus/{{CASE_ID}}/weather.epw
- tests/fixtures/validation/energyplus/{{CASE_ID}}/energyplus-output.raw.csv
- tests/fixtures/validation/energyplus/{{CASE_ID}}/energyplus-output.reference.json
- tests/fixtures/validation/energyplus/{{CASE_ID}}/provenance.json

## Required non-claims

This fixture does not claim:

- exact EnergyPlus numerical equivalence;
- ASHRAE 140 / BESTEST-style validation anchor coverage;
- full ISO 52016 node/matrix solver equivalence.
