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

## Real Application Pipeline

The building energy-balance route and energy-balance report facade use the Energy Calculation Parity annual engine path:

- `GET /api/v1/buildings/{buildingId}/load-calculations/energy-balance`
- `GET /api/v1/reports/buildings/{buildingId}/energy-balance/excel`

`EnergyCalculationPipelineService` calls the existing building energy source as an explicit adapter, converts available monthly/hourly demand into `AnnualEnergyBalanceInput`, and then calls `AnnualEnergyBalanceEngine`. When full hourly generation from weather is not available, the adapter marks the source as `synthetic profile` or `unavailable`; diagnostics are carried into the response/report model.

The mapped output includes annual heating demand, annual cooling demand, monthly heating/cooling values, annual total, EUI when area is known, peak heating/cooling load, diagnostics and assumptions.

## Deterministic Fixtures

- `annual-constant-heating-load.json`
- `annual-constant-cooling-load.json`
- `annual-monthly-aggregation-consistency.json`
- `annual-energy-use-intensity.json`

## Limits

This is an MVP-level engineering annual balance. It does not claim full compliance with any external method.
