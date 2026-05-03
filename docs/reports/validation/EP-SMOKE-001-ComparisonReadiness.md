# EP-SMOKE-001 Comparison Readiness

Generated at: 2026-05-03 06:12:33 UTC

## Status

| Field | Value |
|---|---|
| Case id | EP-SMOKE-001 |
| Name | Single zone transmission-only heating smoke case |
| Stage | Smoke |
| Status | ReferenceFixturePlaceholder |
| Reference status | PlaceholderReferenceOutput |
| Source | Offline committed reference fixture placeholder; real EnergyPlus model/output to be added in a future validation milestone. |

## Engineering input summary

| Field | Value |
|---|---|
| Floor area | 50.0 m² |
| Volume | 150.0 m³ |
| Opaque area | 180.0 m² |
| U-value | 0.35 W/(m²·K) |
| Indoor setpoint | 20.0 °C |
| Outdoor dry-bulb | -5.0 °C |
| Duration | 24 h |
| Expected heat loss | 1575.0 W |
| Expected heating energy | 37.8 kWh |

## Metrics and tolerances

| Metric | Type | Unit | Tolerance percent | Absolute tolerance |
|---|---|---|---:|---:|
| annual-heating-kwh | NumericWithinTolerance | kWh | 20.0 | 0.5 |
| peak-heating-w | NumericWithinTolerance | W | 25.0 | 50.0 |
| annual-cooling-kwh | SameSign | kWh | 0.0 | 0.1 |

## Required non-claims

- Does not claim exact EnergyPlus numerical parity.
- Does not claim ASHRAE 140 validation coverage.
- Does not claim full ISO 52016 node/matrix solver parity.

## Readiness interpretation

EP-SMOKE-001 is ready as a fixture scaffold.

It is not a real EnergyPlus comparison yet.

The current reference output is a placeholder.

Future work must replace or supplement the placeholder with real EnergyPlus model/output files and provenance metadata.

Comparison must remain tolerance-based and must not claim exact EnergyPlus parity or ASHRAE 140 validation coverage.
