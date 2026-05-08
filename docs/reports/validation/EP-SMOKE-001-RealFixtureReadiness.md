# EP-SMOKE-001 Real Fixture Readiness

Generated at: 2026-01-01 00:00:00 UTC

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-001 |
| Status | NotReadyRealFixtureMissingFiles |
| Real fixture ready | False |
| Require real fixture | False |

## Existing placeholder scaffold files

| File | Exists |
|---|---|
| case-metadata.json | True |
| assistantengineer-input.json | True |
| reference-output.placeholder.json | True |
| comparison-tolerances.json | True |

## Required future real fixture files

| File | Exists |
|---|---|
| energyplus-model.idf | False |
| weather.epw | False |
| energyplus-output.raw.csv | False |
| energyplus-output.reference.json | False |
| provenance.json | False |

## Missing real fixture files

- energyplus-model.idf
- weather.epw
- energyplus-output.raw.csv
- energyplus-output.reference.json
- provenance.json

## Interpretation

EP-SMOKE-001 currently remains a placeholder comparison unless all real fixture files are present.

Missing real fixture files do not fail Engineering Core V1 closure.

They only fail when this tool is run with --require-real-fixture.

## Required non-claims

- Does not claim exact EnergyPlus numerical equivalence.
- Does not claim exact StandardReference numerical equivalence.
- Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- Does not claim full ISO 52016 node/matrix solver equivalence.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.
