# Engineering Core V1 Validation Readiness

Generated at: 2026-01-01 00:00:00 UTC

## Registry summary

| Field | Value |
|---|---|
| Registry name | Engineering Core V1 EnergyPlus / ASHRAE 140-style Validation Case Registry |
| Version | v1 |
| Status | PlannedValidation |
| Case count | 5 |
| Smoke cases | 3 |
| ASHRAE 140-style cases | 2 |
| Planned cases | 0 |
| Reference fixture placeholders | 3 |
| Metric count | 13 |

## Default tolerances

| Metric type | Default interpretation |
|---|---|
| NumericWithinTolerance | Compare numeric values with documented tolerance percent and absolute tolerance. |
| DirectionalTrend | Compare expected response direction only. |
| SameSign | Compare positive/negative/zero sign only. |

## Cases

| Case id | Stage | Status | Metrics |
|---|---|---|---:|
| ASHRAE140-STYLE-001 | Ashrae140Style | PlannedValidation | 1 |
| ASHRAE140-STYLE-002 | Ashrae140Style | PlannedValidation | 1 |
| EP-SMOKE-001 | Smoke | ReferenceFixturePlaceholder | 3 |
| EP-SMOKE-002 | Smoke | ReferenceFixturePlaceholder | 4 |
| EP-SMOKE-003 | Smoke | ReferenceFixturePlaceholder | 4 |

## Required non-claims

- This readiness report is not exact EnergyPlus numerical parity.
- This readiness report is not ASHRAE 140 certification.
- This readiness report is not full ISO 52016 node/matrix solver parity.

This registry is ready as a future validation backlog and smoke-fixture scaffold.

It is not exact EnergyPlus numerical parity.

It is not ASHRAE 140 certification.
