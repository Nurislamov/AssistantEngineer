# EP-SMOKE-002 Comparison Result

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-002 |
| Name | EP-SMOKE-002 |
| Stage | Smoke |
| Runner | GenericEnergyPlusValidationFixtureRunner |
| Comparison status | PlaceholderComparison |
| Reference status | PlaceholderReferenceOutput |
| Reference file | tests/fixtures/validation/energyplus/EP-SMOKE-002/reference-output.placeholder.json |
| All metrics passed | True |

## Metrics

| Metric | Type | AssistantEngineer | Reference | Absolute difference | Effective absolute tolerance | Passed |
|---|---|---:|---:|---:|---:|---|
| annual-cooling-kwh | NumericWithinTolerance | 18 | 18 | 0 | 0 | True |
| peak-cooling-w | NumericWithinTolerance | 3600 | 3600 | 0 | 0 | True |
| solar-orientation-response | DirectionalTrend | 1 | 1 | 0 | 0 | True |
| annual-heating-kwh | NumericWithinTolerance | 0 | 0 | 0 | 0 | True |

## Required non-claims

- Does not claim exact EnergyPlus numerical equivalence.
- Does not claim exact StandardReference numerical equivalence.
- Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- Does not claim full ISO 52016 node/matrix solver equivalence.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

## Interpretation

Fixture compared against placeholder reference output only. This is not a real EnergyPlus validation and not an ASHRAE 140 / BESTEST-style validation anchor claim.

PlaceholderComparison is not real EnergyPlus validation.

This is not ASHRAE 140 / BESTEST-style validation anchor coverage.

This does not claim exact EnergyPlus numerical equivalence.

Future work must replace or supplement the placeholder reference with real EnergyPlus model/output files and provenance metadata.
