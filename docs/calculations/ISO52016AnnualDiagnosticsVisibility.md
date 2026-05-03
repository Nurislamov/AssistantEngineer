# ISO 52016 Annual Diagnostics Visibility

Stage 5 makes the annual ISO 52016 result disclose which solar path was used.

## Closed scope

- `Iso52016AnnualEnergyNeedResult` carries calculation diagnostics.
- Annual steady-state results expose `Iso52016.WeatherSolarContextUsed` when the ISO 52016 weather-solar context feeds the heat balance.
- Annual steady-state results expose `Iso52016.SolarGainComponentPathUsed` when window solar gains are fed from beam/diffuse/ground component irradiance.
- Annual steady-state results expose `Iso52016.MatrixSolarRadiationFallbackUsed` when the legacy radiation path is used.
- Perez/weather-solar diagnostics from the context are propagated into the annual result.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical parity.
- This does not claim ASHRAE 140 validation coverage.
- This does not claim full ISO 52016 node/matrix solver parity.
- This does not remove legacy fallback support.

## Guard tests

- `Iso52016AnnualDiagnosticsVisibilityTests.CalculateBuildingEnergyNeedsAsync_ExposesWeatherSolarContextDiagnosticsWhenContextIsAvailable`
- `Iso52016AnnualDiagnosticsVisibilityTests.CalculateBuildingEnergyNeedsAsync_ExposesMatrixFallbackDiagnosticWhenContextIsAbsent`
