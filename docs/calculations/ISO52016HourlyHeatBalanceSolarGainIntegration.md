# ISO 52016 Hourly Heat Balance Solar Gain Integration

Stage 3 closes the next integration seam after the ISO 52010/Perez solar foundation and ISO 52016 weather/solar context.

## Closed scope

- `WindowSolarGainInputFactory` can now build inputs from `Iso52016SurfaceWeatherSolarRecord`.
- `Iso52016HourlyHeatBalanceCalculator.CalculateZoneHourEnergyNeed` accepts an optional `Iso52016HourlyWeatherSolarRecord`.
- When the ISO 52016 hourly weather/solar record is provided, room window solar gains use separated component irradiance:
  - beam/direct surface irradiance;
  - diffuse sky irradiance;
  - ground-reflected irradiance.
- When the ISO 52016 hourly weather/solar record is not provided, the legacy `ISolarRadiationService` path remains available.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical parity.
- This does not claim ASHRAE 140 validation coverage.
- This does not claim full ISO 52016 node/matrix solver parity.
- This does not remove the legacy design-day radiation path yet.

## Guard tests

- `Iso52016HourlyHeatBalanceSolarContextIntegrationTests.HeatBalanceCalculator_UsesIso52016WeatherSolarSurfaceComponentsWhenProvided`
- `Iso52016HourlyHeatBalanceSolarContextIntegrationTests.HeatBalanceCalculator_ClampsIso52016WeatherSolarNightComponentsBeforeSolarGain`
