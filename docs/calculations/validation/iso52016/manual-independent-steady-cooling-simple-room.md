# Manual independent steady cooling simple room

- Fixture id: `manual-independent-steady-cooling-simple-room`
- Source kind: `ManualIndependent`
- Calculation path: `MatrixReduced`

## Assumptions

- Single-zone steady cooling anchor with constant outdoor temperature.
- Internal and solar gains are treated as additive cooling loads.
- Validation/internal engineering anchors only.

## Manual independent equations

1. Temperature difference:
   - `deltaT = outdoorTemperatureC - coolingSetpointC`
2. Peak cooling load:
   - `peakCoolingW = max(0, uaWPerK * deltaT + internalGainW + solarGainW)`
3. Monthly cooling energy:
   - `monthlyCoolingKWh = peakCoolingW * coolingHoursPerMonth / 1000`
4. Annual cooling energy:
   - `annualCoolingKWh = monthlyCoolingKWh * 12`
5. Operative temperatures:
   - `mean = coolingSetpointC + meanOperativeOffsetC`
   - `max = coolingSetpointC + maxOperativeOffsetC`
   - `min = coolingSetpointC + minOperativeOffsetC`

## Step-by-step arithmetic

- `deltaT = 34.0 - 25.0 = 9.0 C`
- `peakCoolingW = 227.7777778 * 9.0 + 450.0 + 300.0 = 2800.0 W`
- `monthlyCoolingKWh = 2800.0 * 71.4285714 / 1000 = 200.0 kWh`
- `annualCoolingKWh = 200.0 * 12 = 2400.0 kWh`
- `mean/max/min operative = 25.0 / 25.4 / 24.8 C`

## Expected results

- AnnualHeatingKWh: `0.0`
- AnnualCoolingKWh: `2400.0`
- PeakHeatingW: `0.0`
- PeakCoolingW: `2800.0`
- MeanOperativeTemperatureC: `25.0`
- MaxOperativeTemperatureC: `25.4`
- MinOperativeTemperatureC: `24.8`
- HourlyResultCount: `24`
- MonthlyHeatingKWh: `12 x 0.0`
- MonthlyCoolingKWh: `12 x 200.0`

## Tolerance rationale

- Absolute tolerance `0.75` and relative tolerance `1.25%` capture low-level rounding noise but still enforce meaningful validation anchors.

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
