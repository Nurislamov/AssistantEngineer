# Load Aggregation

This Energy Calculation equivalence step aggregates deterministic room load results to thermal zones, floors and buildings.

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

## Real Application Pipeline

Floor and building load routes use room results from the same Energy Calculation equivalence pipeline and aggregate them with `LoadAggregationEngine`.

- `GET /api/v1/floors/{floorId}/load-calculations/heating-load`
- `GET /api/v1/floors/{floorId}/load-calculations/cooling-load`
- `GET /api/v1/buildings/{buildingId}/load-calculations/heating-load`
- `GET /api/v1/buildings/{buildingId}/load-calculations/cooling-load`

`EnergyCalculationPipelineService` assembles room load inputs, calculates each room with `RoomLoadCalculationEngine`, then passes unique room load records to `LoadAggregationEngine`. Floor aggregation only includes rooms on the requested floor. Building aggregation includes the building rooms once and uses thermal-zone ids only for grouping/breakdown, not for double counting.

The current API path uses design-point aggregation. If hourly profiles are not available, diagnostics identify design-point aggregation instead of silently claiming coincident hourly behavior.

Public method query values are carried as `requestedMethod`; `actualMethod` is `ExternalReferenceValidationDesignPoint` for the current floor/building load routes. Component breakdowns include ground when room load results contain ground components.

## Deterministic Fixtures

- `aggregation-floor-two-rooms.json`
- `aggregation-building-two-floors.json`
- `aggregation-thermal-zone-no-double-count.json`

## Limits

Aggregation does not recalculate room physics. It consumes room load results and preserves deterministic grouping rules.
