# Equipment Sizing Integration

This Energy Calculation Parity step links calculated loads to HVAC equipment sizing.

## Scope

Equipment sizing is separate from internal equipment heat gains. Internal equipment gains are heat gains inside rooms. Equipment sizing selects HVAC capacity against required heating and cooling loads.

## Formula

```text
requiredCapacityWithReserveW = requiredLoadW * safetyFactor
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

Room equipment selection uses the Energy Calculation Parity load and sizing path:

- `POST /api/v1/rooms/{roomId}/equipment-selection`
- cooling report equipment rows, when a system type and unit type are requested

The route calls `ILoadCalculationsFacade.CalculateRoomEquipmentSizingAsync`. The pipeline calculates the actual room heating and cooling load with `RoomLoadCalculationEngine`, applies the project safety factor, queries the active equipment catalog through the sizing provider, and evaluates candidates with `EquipmentSizingEngine`.

The API response keeps compatibility fields and maps sizing evidence:

- `RequiredCoolingCapacityW`
- `RequiredHeatingCapacityW`
- `CapacityWithReserveW`
- `SafetyFactor`
- accepted/recommended candidates
- rejected candidates with reasons
- best match
- diagnostics

Heating capacity is evaluated when catalog rows expose heating capacity. If the active catalog does not expose heating capacity, the result still reports the calculated required heating load and adds a diagnostic: heating sizing is skipped because catalog items do not expose heating capacity. Heating capacity is not inferred from cooling capacity.

## Deterministic Fixtures

- `equipment-sizing-cooling-simple.json`
- `equipment-candidate-accepted.json`
- `equipment-candidate-rejected.json`
- `equipment-no-equipment-found.json`

## Limits

The deterministic engine does not duplicate catalog persistence or catalog query logic. Heating selection quality depends on catalog heating capacity data being present.
