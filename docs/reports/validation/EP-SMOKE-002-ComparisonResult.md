# EP-SMOKE-002 Comparison Result

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-002 |
| Name | Single zone solar cooling smoke case |
| Stage | Smoke |
| Runner | GenericEnergyPlusValidationFixtureRunner |
| Comparison status | PlaceholderComparison |
| Reference status | PlaceholderReferenceOutput |
| Reference file | D:/Project/AssistantEngineer/tests/fixtures/validation/energyplus/EP-SMOKE-002/reference-output.placeholder.json |
| All metrics passed | True |

## Metrics

| Metric | Type | AssistantEngineer | Reference | Absolute difference | Effective absolute tolerance | Passed |
|---|---|---:|---:|---:|---:|---|
| annual-cooling-kwh | NumericWithinTolerance | 18 | 18 | 0 | 3.6 | True |
| peak-cooling-w | NumericWithinTolerance | 3600 | 3600 | 0 | 900 | True |
| solar-orientation-response | DirectionalTrend | 1 | 1 | 0 | 0 | True |
| annual-heating-kwh | SameSign | 0 | 0 | 0 | 0.1 | True |

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- Does not claim full optical glazing or EnergyPlus solar distribution parity.

## Interpretation

Fixture compared against placeholder reference output only. This is not a real EnergyPlus validation and not an ASHRAE 140 validation claim.

PlaceholderComparison is not real EnergyPlus validation.

This is not ASHRAE 140 validation coverage.

This does not claim exact EnergyPlus numerical parity.

Future work must replace or supplement placeholder references with real EnergyPlus model/output files and provenance metadata.
