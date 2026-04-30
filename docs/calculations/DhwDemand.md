# DHW Demand

This Energy Calculation Parity step calculates domestic hot water demand.

## Formula

```text
dailyLiters = people * dailyLitersPerPerson
energyKWh = liters * densityKgPerLiter * cpKJPerKgK * deltaT / 3600
```

Current constants:

- density = 1.0 kg/liter
- cp = 4.186 kJ/(kg K)

## Monthly And Annual Demand

Monthly demand uses a non-leap year month calendar. Annual demand is the sum of monthly values.

## Diagnostics

Zero occupancy returns zero demand with diagnostics. Hot water temperature must be greater than cold water temperature.

## Deterministic Fixtures

- `dhw-residential-simple.json`
- `dhw-zero-occupancy.json`

## Limits

This is a simplified DHW demand model. Distribution, circulation and storage losses are scalar inputs.
