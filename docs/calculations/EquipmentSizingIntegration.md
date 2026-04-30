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

## Deterministic Fixtures

- `equipment-sizing-cooling-simple.json`
- `equipment-candidate-accepted.json`
- `equipment-candidate-rejected.json`
- `equipment-no-equipment-found.json`

## Limits

The deterministic engine does not duplicate catalog persistence or catalog query logic.
