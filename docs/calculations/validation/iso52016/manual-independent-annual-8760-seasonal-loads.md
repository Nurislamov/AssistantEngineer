# Manual independent annual 8760 seasonal loads

- Fixture id: `manual-independent-annual-8760-seasonal-loads`
- Source kind: `ManualIndependent`
- Calculation path: `PhysicalNodeModel`

## Assumptions

- Synthetic annual profile contains `8760` hourly records.
- Monthly heating and cooling totals are manually prepared independent reference values.
- Annual totals are arithmetic sums of monthly values.
- Peak values and operative temperatures are manually bounded by independent engineering assumptions for this anchor.

## Manual independent equations

1. Annual heating:
   - `annualHeatingKWh = sum(monthlyHeatingKWhByHand[1..12])`
2. Annual cooling:
   - `annualCoolingKWh = sum(monthlyCoolingKWhByHand[1..12])`
3. Peak values:
   - `peakHeatingW = peakHeatingWByHand`
   - `peakCoolingW = peakCoolingWByHand`
4. Operative temperatures:
   - `mean = meanOperativeTemperatureCByHand`
   - `max = maxOperativeTemperatureCByHand`
   - `min = minOperativeTemperatureCByHand`
5. Hourly count:
   - `hourlyResultCount = hourlyProfileLength`

## Step-by-step arithmetic

- Heating sum:
  - `650 + 610 + 560 + 430 + 300 + 180 + 110 + 130 + 280 + 480 + 620 + 750 = 5100.0 kWh`
- Cooling sum:
  - `60 + 90 + 160 + 260 + 380 + 470 + 520 + 500 + 360 + 220 + 120 + 60 = 3200.0 kWh`
- Peak values:
  - `peakHeatingW = 4100.0 W`
  - `peakCoolingW = 3600.0 W`
- Operative temperatures:
  - `mean/max/min = 23.2 / 27.8 / 18.6 C`
- Hourly result count:
  - `8760`

## Expected results

- AnnualHeatingKWh: `5100.0`
- AnnualCoolingKWh: `3200.0`
- PeakHeatingW: `4100.0`
- PeakCoolingW: `3600.0`
- MeanOperativeTemperatureC: `23.2`
- MaxOperativeTemperatureC: `27.8`
- MinOperativeTemperatureC: `18.6`
- HourlyResultCount: `8760`
- MonthlyHeatingKWh: `[650, 610, 560, 430, 300, 180, 110, 130, 280, 480, 620, 750]`
- MonthlyCoolingKWh: `[60, 90, 160, 260, 380, 470, 520, 500, 360, 220, 120, 60]`

## Tolerance rationale

- Absolute tolerance `2.0` and relative tolerance `2.5%` are used because this case aggregates annual and monthly synthetic loads and should tolerate only small numeric drift.

## Non-claims

- Validation/internal engineering anchors only.
- Manual independent reference fixtures only.
- No full ISO 52016 parity claim.
- No pyBuildingEnergy parity claim.
- No EnergyPlus parity claim.
- No ASHRAE 140 validation claim.
- ExternalParityCovered is not allowed in this stage.
- This is not a parity claim.
- This is not external certification.
