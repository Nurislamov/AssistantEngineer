# Manual independent steady heating simple room

- Fixture id: `manual-independent-steady-heating-simple-room`
- Source kind: `ManualIndependent`
- Calculation path: `MatrixReduced`

## Assumptions

- Single-zone steady heating anchor with constant outdoor temperature.
- Internal and solar gains are treated as direct offsets in the steady load balance.
- Validation/internal engineering anchors only.

## Manual independent equations

1. Temperature difference:
   - `deltaT = heatingSetpointC - outdoorTemperatureC`
2. Peak heating load:
   - `peakHeatingW = max(0, uaWPerK * deltaT - internalGainW - solarGainW)`
3. Monthly heating energy:
   - `monthlyHeatingKWh = peakHeatingW * heatingHoursPerMonth / 1000`
4. Annual heating energy:
   - `annualHeatingKWh = monthlyHeatingKWh * 12`
5. Operative temperatures:
   - `mean = heatingSetpointC + meanOperativeOffsetC`
   - `max = heatingSetpointC + maxOperativeOffsetC`
   - `min = heatingSetpointC + minOperativeOffsetC`

## Step-by-step arithmetic

- `deltaT = 21.0 - (-5.0) = 26.0 C`
- `peakHeatingW = 86.5384615 * 26.0 - 150.0 - 0.0 = 2100.0 W`
- `monthlyHeatingKWh = 2100.0 * 71.4285714 / 1000 = 150.0 kWh`
- `annualHeatingKWh = 150.0 * 12 = 1800.0 kWh`
- `mean/max/min operative = 21.0 / 21.3 / 20.7 C`

## Expected results

- AnnualHeatingKWh: `1800.0`
- AnnualCoolingKWh: `0.0`
- PeakHeatingW: `2100.0`
- PeakCoolingW: `0.0`
- MeanOperativeTemperatureC: `21.0`
- MaxOperativeTemperatureC: `21.3`
- MinOperativeTemperatureC: `20.7`
- HourlyResultCount: `24`
- MonthlyHeatingKWh: `12 x 150.0`
- MonthlyCoolingKWh: `12 x 0.0`

## Tolerance rationale

- Absolute tolerance `0.5` and relative tolerance `1.0%` allow tiny rounding differences in floating-point arithmetic while keeping the anchor numerically strict.

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
