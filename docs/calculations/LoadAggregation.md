# Load Aggregation

This Energy Calculation Parity step aggregates deterministic room load results to thermal zones, floors and buildings.

## Design-Point Mode

```text
targetHeatingLoadW = sum(roomHeatingLoadW)
targetCoolingLoadW = sum(roomCoolingLoadW)
targetAreaM2 = sum(roomAreaM2)
```

Room ids are de-duplicated before summing to avoid double counting.

## Hourly Mode

If hourly room profiles are supplied, the peak is calculated from the coincident sum at each hour:

```text
peakLoadW = max(sum(roomLoadW at same hour))
```

If hourly profiles are not available, diagnostics report that design-point aggregation was used.

## Breakdown

The aggregation result includes component sums for transmission, solar, ventilation, infiltration, internal gains and ground.

## Deterministic Fixtures

- `aggregation-floor-two-rooms.json`
- `aggregation-building-two-floors.json`
- `aggregation-thermal-zone-no-double-count.json`

## Limits

Aggregation does not recalculate room physics. It consumes room load results and preserves deterministic grouping rules.
