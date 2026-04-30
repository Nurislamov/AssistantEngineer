# Room Heating / Cooling Load

This Energy Calculation Parity step combines completed room-level components into a deterministic room load result.

## Scope

- Heating components: transmission, window transmission, ground, ventilation, infiltration.
- Cooling components: transmission, window transmission, solar, ventilation, infiltration, internal gains, ground.
- Heating and cooling totals are never allowed to become negative.
- Solar gains and internal gains are separate cooling components. They are not deducted from heating load in the current design-point method.

## Formula

Heating load:

```text
heatingLoadW =
  transmissionW
  + windowTransmissionW
  + groundW
  + ventilationW
  + infiltrationW
```

Cooling load:

```text
coolingLoadW =
  transmissionW
  + windowTransmissionW
  + solarW
  + ventilationW
  + infiltrationW
  + internalGainsW
  + groundW
```

When `areaM2 > 0`:

```text
loadWPerM2 = loadW / areaM2
```

## Diagnostics

Missing component inputs are reported as zero-component diagnostics. Invalid room area is an error. Negative fixed deterministic components are clamped to zero and reported.

## Deterministic Fixtures

- `room-load-heating-transmission-only.json`
- `room-load-heating-transmission-ventilation-infiltration.json`
- `room-load-cooling-solar-internal-ventilation.json`
- `room-load-does-not-go-negative.json`

## Limits

This is a design-point component aggregation engine. It does not claim full dynamic simulation and does not apply useful internal or solar gain offsets to heating load.
