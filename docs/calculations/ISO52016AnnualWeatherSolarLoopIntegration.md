# ISO 52016 Annual Weather/Solar Loop Integration

Stage 4 wires the ISO 52016 weather/solar context into the annual hourly steady-state loop.

## Closed scope

- `Iso52016HourlyWeatherContext` carries an optional `Iso52016WeatherSolarContext`.
- `Iso52016HourlyWeatherProvider` can build that context through `IIso52016WeatherSolarContextBuilder`.
- `Iso52016HourlySteadyStateCalculator` passes `weatherContext.WeatherSolarContext?.GetHour(weather.HourOfYear)` into `Iso52016HourlyHeatBalanceCalculator`.
- The legacy `ISolarRadiationService` path remains available when the weather/solar context is not provided.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical parity.
- This does not claim ASHRAE 140 validation coverage.
- This does not claim full ISO 52016 node/matrix solver parity.
- This does not remove the legacy design-day radiation path yet.

## Guard test

- `Iso52016HourlySteadyStateWeatherSolarContextIntegrationTests.CalculateBuildingEnergyNeedsAsync_PassesWeatherSolarHoursIntoHeatBalance`
