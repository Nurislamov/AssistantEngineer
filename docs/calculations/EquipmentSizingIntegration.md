# Equipment Sizing Integration

This Energy Calculation equivalence step links calculated loads to HVAC equipment sizing.

## Scope

Equipment sizing is separate from internal equipment heat gains. Internal equipment gains are heat gains inside rooms. Equipment sizing selects HVAC capacity against required heating and cooling loads.

## Formula

```text
requiredHeatingCapacityWithReserveW = requiredHeatingLoadW * heatingSafetyFactor
requiredCoolingCapacityWithReserveW = requiredCoolingLoadW * coolingSafetyFactor
marginW = equipmentCapacityW - requiredCapacityWithReserveW
marginPercent = marginW / requiredCapacityWithReserveW * 100
```

## Candidate Rules

Candidates are rejected with explicit reasons:

- insufficient cooling capacity
- insufficient heating capacity
- inactive equipment
- wrong type
- missing capacity
- no matching mode

The best match is the accepted candidate with the smallest positive reserve.

## Real Application Pipeline

Room equipment selection uses the Energy Calculation equivalence load and sizing path:

- `POST /api/v1/rooms/{roomId}/equipment-selection`
- cooling report equipment rows, when a system type and unit type are requested

The route calls `ILoadCalculationsFacade.CalculateRoomEquipmentSizingAsync`. The pipeline calculates the actual room heating and cooling load with `RoomLoadCalculationEngine`, applies the project heating safety factor to heating capacity and the project cooling safety factor to cooling capacity, queries the active equipment catalog through the sizing provider, and evaluates candidates with `EquipmentSizingEngine`.

The API response keeps compatibility fields and maps sizing evidence:

- `RequiredCoolingCapacityW`
- `RequiredHeatingCapacityW`
- `CapacityWithReserveW`
- `SafetyFactor`
- `HeatingSafetyFactor`
- `CoolingSafetyFactor`
- accepted/recommended candidates
- rejected candidates with reasons
- best match
- diagnostics

`SafetyFactor` is retained as a backward-compatible response field. New application code should read `HeatingSafetyFactor` and `CoolingSafetyFactor` because reserve capacity is calculated separately for heating and cooling.

Heating capacity is evaluated against `RequiredHeatingCapacityWithReserveW` when catalog rows expose heating capacity. Cooling capacity is evaluated against `RequiredCoolingCapacityWithReserveW` when catalog rows expose cooling capacity. If the active catalog does not expose heating capacity, the result still reports the calculated required heating load and adds a diagnostic: heating sizing is skipped because catalog items do not expose heating capacity. Heating capacity is not inferred from cooling capacity.

Diagnostics include `EquipmentSizing.HeatingSafetyFactorApplied` and `EquipmentSizing.CoolingSafetyFactorApplied` with the actual factor values.

## Deterministic Fixtures

- `equipment-sizing-cooling-simple.json`
- `equipment-candidate-accepted.json`
- `equipment-candidate-rejected.json`
- `equipment-no-equipment-found.json`

## Limits

The deterministic engine does not duplicate catalog persistence or catalog query logic. Heating selection quality depends on catalog heating capacity data being present.
This remains an Energy Calculation equivalence deterministic sizing pipeline; it does not claim external equivalence coverage.
