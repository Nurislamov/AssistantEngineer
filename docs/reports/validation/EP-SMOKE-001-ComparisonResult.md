# EP-SMOKE-001 Comparison Result

Generated at: 2026-05-02 20:30:35 UTC

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-001 |
| Name | Single zone transmission-only heating smoke case |
| Stage | Smoke |
| Comparison status | PlaceholderComparison |
| Reference status | PlaceholderReferenceOutput |
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

EP-SMOKE-001 placeholder comparison passed tolerance checks against placeholder reference outputs.

This is not a real EnergyPlus validation.

This is not ASHRAE 140 validation coverage.

This does not claim exact EnergyPlus numerical parity.

Future work must replace or supplement the placeholder reference with real EnergyPlus model/output files and provenance metadata.
