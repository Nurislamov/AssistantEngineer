# Validation Tolerance Policy

## Purpose

This policy defines how expected and actual numeric values are compared for validation fixtures and engineering regression anchors.
It prevents arbitrary tolerance selection and requires explicit, reviewable tolerance rationale.
Units used in validation tolerances follow `docs/engineering/units-governance.md` and `docs/engineering/units-registry.json`.

## Scope

This policy applies to:

- Tier 0 internal deterministic anchors.
- Tier 1 manual engineering cases.
- Tier 2 published benchmark-style fixtures.
- Tier 3 external tool diagnostic comparisons.
- Future Tier 4 validation only when a formal protocol defines tolerances.

## Non-claims

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No pyBuilding\u0045nergy parity claim.
- No full ISO/EN compliance claim.
- No certified/certification claim.

## General numeric comparison rule

A value passes if:

- absoluteError <= absoluteTolerance
- OR
- relativeError <= relativeTolerance

Where:

- absoluteError = abs(actual - expected)
- relativeError = absoluteError / max(abs(expected), relativeFloor)

Required baseline:

- relativeFloor = 1e-9

## Zero and near-zero values

- For expected values near zero, absolute tolerance is authoritative.
- Relative tolerance must not be used alone near zero.
- Zero expected values must declare explicit absolute tolerance.

## Units policy

All fixture values must declare units explicitly.

Required unit coverage for this policy:

- W
- kW
- Wh
- kWh
- °C
- K
- m²
- m³
- m³/h
- ACH
- W/(m²·K)
- W/m²

Rules:

- No hidden W/kW conversion in fixtures.
- expected-output.json should store unit-explicit property names where practical.
- derivation.md must show unit conversions.

## Recommended default tolerances

| Case type | Metric | Absolute tolerance | Relative tolerance | Notes |
| --- | --- | --- | --- | --- |
| Manual exact arithmetic cases | W | 1e-6 W | 1e-6 | Floating-point representation only |
| Manual exact arithmetic cases | kW | 1e-9 kW | 1e-6 | Floating-point representation only |
| Manual exact arithmetic cases | Wh | 1e-6 Wh | 1e-6 | Floating-point representation only |
| Manual exact arithmetic cases | kWh | 1e-9 kWh | 1e-6 | Floating-point representation only |
| Manual exact arithmetic cases | temperature K/°C | 1e-9 | not preferred | Prefer absolute only |
| Manual exact arithmetic cases | dimensionless efficiencies/factors | 1e-9 | 1e-9 | Efficiency/factor comparisons |

Hourly numerical simulation anchors:

- Default absolute tolerance must be declared per metric.
- Default relative tolerance must be declared per metric.
- No global default unless documented.

Annual/monthly aggregation:

- Aggregation tolerance must account for sum of hourly tolerances.
- Annual expected values should declare both absolute and relative tolerances.

External diagnostic comparison:

- Tolerance is a diagnostic threshold, not proof of equivalence.
- Tolerance must be metric-specific.
- Mismatch classification must be included in evidence.

## Rounding policy

- expected-output.json should store full precision used by test.
- derivation.md may show rounded human-readable values but must also state exact expected values.
- Tests should compare numeric values, not formatted strings.
- No rounding before comparison unless a fixture explicitly declares rounding.

## Tolerance file schema

Recommended comparison-tolerances.json shape:

```json
{
  "relativeFloor": 1e-9,
  "relativeTolerance": 0.000001,
  "absoluteTolerances": {
    "W": 0.000001,
    "kWh": 0.000000001
  },
  "rationale": "Exact arithmetic simple case; tolerances only account for floating-point representation."
}
```

Existing fixtures are not required to migrate immediately to this exact schema if their current declared tolerances are explicit and tests pass.

## Fixture requirements

Every validation fixture must include:

- case-metadata.json
- derivation.md
- input.json
- expected-output.json
- comparison-tolerances.json

Every validation fixture must:

- declare units;
- declare excluded effects;
- declare non-claims;
- provide tolerance rationale;
- avoid production-code-derived expected values for Tier 1 manual engineering cases.
- declare assumptions and cross-reference `docs/engineering/engineering-assumptions-registry.md` when applicable.

## Promotion rules

- Tier 1 manual fixtures cannot be promoted to Tier 2 unless tolerances and provenance are explicit.
- Tier 3 external comparisons cannot be described as parity or equivalence without governance approval.
- Any positive validation claim requires dedicated evidence.
