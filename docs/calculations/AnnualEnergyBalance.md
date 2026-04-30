# Annual Energy Balance

This Energy Calculation Parity step provides a deterministic annual balance from hourly load inputs. In application endpoints it can also run behind an explicit adapter when only monthly source data is available.

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

Synthetic weather use is reported. Missing hourly records and invalid area are errors. Negative hourly loads are clamped to zero and reported. Application results expose:

- `energyDataSource`: `TrueHourlySimulation`, `MonthlyBalanceAdapter`, `DeterministicFixture`, or `Unavailable`;
- `isTrueHourly8760`: true only when the source is actual hourly simulation and exactly 8760 records are supplied;
- `hourlyRecordCount`: the number of records consumed by the annual engine;
- diagnostics for representative monthly records when the monthly adapter is used.

## Real Application Pipeline

The building energy-balance route and energy-balance report facade use the Energy Calculation Parity annual engine path:

- `GET /api/v1/buildings/{buildingId}/load-calculations/energy-balance`
- `GET /api/v1/reports/buildings/{buildingId}/energy-balance/excel`

`EnergyCalculationPipelineService` calls the existing building energy source as an explicit adapter, converts available monthly/hourly demand into `AnnualEnergyBalanceInput`, and then calls `AnnualEnergyBalanceEngine`.

If the existing hourly simulation path provides 8760 hourly records, `HourlySimulationToAnnualEnergyInputMapper` maps them into annual engine input, the result is marked as `TrueHourlySimulation`, `hourlyRecordCount = 8760`, and `isTrueHourly8760 = true`. If the hourly source is unavailable but monthly balances exist, the adapter generates representative monthly records, marks the source as `MonthlyBalanceAdapter`, sets `isTrueHourly8760 = false`, and emits a diagnostic saying this is not a true 8760 simulation. If neither hourly nor monthly source data is available, the application path returns validation instead of fake zero annual results.

The mapper carries available hourly heating/cooling, transmission, ventilation, ground, solar and internal-gain values. In the current true hourly path, ventilation is reported as the combined hourly ventilation heat-transfer contribution. Infiltration is not separately exposed by that path yet, so it remains `0` and diagnostics state that the annual infiltration breakdown is partial instead of inventing a split.

The separate building energy analysis API remains labelled as `ISO52016InspiredHourlyAnalysis`/monthly analysis when it uses that hourly/monthly service path. It is not silently mixed with the load-calculations annual adapter.

The mapped output includes annual heating demand, annual cooling demand, monthly heating/cooling values, annual total, EUI when area is known, peak heating/cooling load, diagnostics and assumptions.

## Deterministic Fixtures

- `annual-constant-heating-load.json`
- `annual-constant-cooling-load.json`
- `annual-monthly-aggregation-consistency.json`
- `annual-energy-use-intensity.json`

## Limits

The load-calculations endpoint is an annual aggregation adapter unless the upstream source supplies true 8760 hourly records. `TrueHourlySimulation` means the project consumed an existing hourly simulation output; it does not claim full compliance with any external method or `ExternalParityCovered` status. Separate infiltration reporting remains a known limitation of the current hourly component breakdown.
