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
| Planned cases | 2 |
| Reference fixture placeholders | 3 |
| Metric count | 13 |

## Default tolerances

| Metric | Tolerance |
|---|---|
| Annual heating energy | 20% |
| Annual cooling energy | 20% |
| Peak heating load | 25% |
| Peak cooling load | 25% |
| Directional trend | directional only |
| Same sign | same sign only |

## Validation cases

| CaseId | Stage | Status | Metrics | Name |
|---|---|---|---:|---|
| ASHRAE140-STYLE-001 | Ashrae140Style | Planned | 2 | Lightweight vs heavyweight envelope sensitivity |
| ASHRAE140-STYLE-002 | Ashrae140Style | Planned | 2 | Window orientation solar sensitivity |
| EP-SMOKE-001 | Smoke | ReferenceFixturePlaceholder | 3 | Single zone transmission-only heating smoke case |
| EP-SMOKE-002 | Smoke | ReferenceFixturePlaceholder | 3 | Single zone solar cooling smoke case |
| EP-SMOKE-003 | Smoke | ReferenceFixturePlaceholder | 3 | Single zone internal gains cooling smoke case |

## Metrics

| CaseId | MetricId | Type | Unit | Tolerance percent |
|---|---|---|---|---:|
| ASHRAE140-STYLE-001 | cooling-mass-response | DirectionalTrend | direction | 0 |
| ASHRAE140-STYLE-001 | heating-mass-response | DirectionalTrend | direction | 0 |
| ASHRAE140-STYLE-002 | orientation-cooling-response | DirectionalTrend | direction | 0 |
| ASHRAE140-STYLE-002 | solar-gain-response | DirectionalTrend | direction | 0 |
| EP-SMOKE-001 | annual-heating-kwh | NumericWithinTolerance | kWh | 20 |
| EP-SMOKE-001 | peak-heating-w | NumericWithinTolerance | W | 25 |
| EP-SMOKE-001 | annual-cooling-kwh | SameSign | kWh | 0 |
| EP-SMOKE-002 | annual-cooling-kwh | NumericWithinTolerance | kWh | 20 |
| EP-SMOKE-002 | peak-cooling-w | NumericWithinTolerance | W | 25 |
| EP-SMOKE-002 | solar-orientation-response | DirectionalTrend | direction | 0 |
| EP-SMOKE-003 | annual-cooling-kwh | NumericWithinTolerance | kWh | 20 |
| EP-SMOKE-003 | annual-heating-kwh | SameSign | kWh | 0 |
| EP-SMOKE-003 | internal-gain-response | DirectionalTrend | direction | 0 |

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.

## Readiness interpretation

This registry is ready as a future validation backlog and smoke-fixture scaffold.

It is not exact EnergyPlus numerical parity.

It is not ASHRAE 140 certification.

It does not claim full ISO 52016 node/matrix solver parity.

Real external validation requires future committed EnergyPlus/reference model files, exported reference outputs, documented tolerances and passing comparison tests.
