# EP-SMOKE-001 Comparison Result

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-001 |
| Name | Single zone transmission-only heating smoke case |
| Stage | Smoke |
| Runner | GenericEnergyPlusValidationFixtureRunner |
| Comparison status | PlaceholderComparison |
| Reference status | PlaceholderReferenceOutput |
| Reference file | D:/Project/AssistantEngineer/tests/fixtures/validation/energyplus/EP-SMOKE-001/reference-output.placeholder.json |
| All metrics passed | True |

## Metrics

| Metric | Type | AssistantEngineer | Reference | Absolute difference | Effective absolute tolerance | Passed |
|---|---|---:|---:|---:|---:|---|
| annual-heating-kwh | NumericWithinTolerance | 37.8 | 37.8 | 0 | 7.56 | True |
| peak-heating-w | NumericWithinTolerance | 1575 | 1575 | 0 | 393.75 | True |
| annual-cooling-kwh | SameSign | 0 | 0 | 0 | 0.1 | True |

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.

## Interpretation

Fixture compared against placeholder reference output only. This is not a real EnergyPlus validation and not an ASHRAE 140 validation claim.

PlaceholderComparison is not real EnergyPlus validation.

This is not ASHRAE 140 validation coverage.

This does not claim exact EnergyPlus numerical parity.

Future work must replace or supplement placeholder references with real EnergyPlus model/output files and provenance metadata.
