# ISO 52016 Weather/Solar Integration

Stage 2 closes the integration seam between the ISO 52010-style solar foundation and the ISO 52016 weather-solar context.

## Closed scope

- ISO 52016 context is built from the annual weather/solar profile.
- Surface irradiance diagnostics from the Perez anisotropic sky model are propagated into the ISO 52016 context diagnostics.
- Window solar gains consume separated component irradiance:
  - beam/direct surface irradiance;
  - diffuse sky irradiance;
  - ground-reflected irradiance.
- Nighttime radiation is clamped before window solar gain calculation.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical parity.
- This does not claim ASHRAE 140 validation coverage.
- This does not claim full ISO 52016 node/matrix solver parity.
- This does not replace future tolerance-based external validation.

## Guard tests

- `Iso52016WeatherSolarContextPerezDiagnosticsTests.Build_PropagatesPerezAnisotropicDiagnostics`
- `Iso52016WeatherSolarWindowGainIntegrationTests.Build_ProvidesPerezComponentIrradianceToWindowSolarGainCalculator`
- `Iso52016WeatherSolarWindowGainIntegrationTests.Build_KeepsNightZeroClampBeforeWindowSolarGainCalculation`
