# Annual 8760 Energy Balance

This Energy Calculation Parity step provides a deterministic annual balance from hourly load inputs.

## Formula

```text
energyKWh = sum(loadW * hourDurationH) / 1000
annualTotalKWh = sum(monthlyTotalKWh)
EUI = annualTotalKWh / buildingAreaM2
```

Monthly totals are built from hourly records grouped by month. Annual totals are the sum of monthly totals.

## Output

The result includes annual heating, annual cooling, annual total, monthly heating/cooling/total arrays, peak heating and cooling load, peak hours, EUI, component energy breakdown and diagnostics.

## Diagnostics

Synthetic weather use is reported. Missing hourly records and invalid area are errors. Negative hourly loads are clamped to zero and reported.

## Deterministic Fixtures

- `annual-constant-heating-load.json`
- `annual-constant-cooling-load.json`
- `annual-monthly-aggregation-consistency.json`
- `annual-energy-use-intensity.json`

## Limits

This is an MVP-level engineering annual balance. It does not claim full compliance with any external method.
