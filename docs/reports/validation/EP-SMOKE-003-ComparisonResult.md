# EP-SMOKE-003 Comparison Result

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-003 |
| Name | Single zone internal gains cooling smoke case |
| Stage | Smoke |
| Runner | GenericEnergyPlusValidationFixtureRunner |
| Comparison status | PlaceholderComparison |
| Reference status | PlaceholderReferenceOutput |
| Reference file | tests/fixtures/validation/energyplus/EP-SMOKE-003/reference-output.placeholder.json |
| All metrics passed | True |

## Metrics

| Metric | Type | AssistantEngineer | Reference | Absolute difference | Effective absolute tolerance | Passed |
|---|---|---:|---:|---:|---:|---|
| annual-cooling-kwh | NumericWithinTolerance | 28,8 | 28,8 | 0 | 5,76 | True |
| peak-cooling-w | NumericWithinTolerance | 1200 | 1200 | 0 | 300 | True |
| internal-gain-response | DirectionalTrend | 1 | 1 | 0 | 0 | True |
| annual-heating-kwh | SameSign | 0 | 0 | 0 | 0,1 | True |

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.
- PlaceholderComparison is not real EnergyPlus validation.
- Future real validation must remain tolerance-based.

## Interpretation

Fixture compared against placeholder reference output only. This is not a real EnergyPlus validation and not an ASHRAE 140 validation claim.

PlaceholderComparison is not real EnergyPlus validation.

This is not ASHRAE 140 validation coverage.

This does not claim exact EnergyPlus numerical parity.

Future work must replace or supplement the placeholder reference with real EnergyPlus model/output files and provenance metadata.
