# ISO 52010 Legacy SolarRadiationService Cleanup

Stage 12 cleans up the legacy `SolarRadiationService.CalculateVerticalSurfaceRadiation(...)` compatibility path.

## Problem

The main ISO52010/Perez -> ISO52016 pipeline already uses proper timestamp, longitude and timezone inputs through `AnnualWeatherSolarProfileBuilder`.

The older compatibility/helper path still constructed solar position inputs with:

- fixed `year: 2026`;
- fixed `offset: TimeSpan.Zero`;
- fixed `LongitudeDegrees: 0`.

That made the legacy path methodically weaker even though it no longer drives the main annual weather-solar pipeline.

## Closed scope

- `ISolarRadiationService` now exposes a location/time-aware overload.
- `SolarRadiationService` uses provided latitude, longitude, timezone offset, year, day of year and hour.
- The old 5-parameter method remains as a compatibility wrapper.
- ISO52016 hourly heat-balance legacy fallback passes configured longitude/timezone/year from `Iso52016EnergyNeedOptions`.

## Explicit non-claims

- This does not make the legacy path the preferred annual ISO52016 path.
- The preferred annual path remains `AnnualWeatherSolarProfileBuilder -> Iso52016WeatherSolarContext`.
- This does not claim exact EnergyPlus numerical equivalence.
- This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.

## Guard tests

- `SolarRadiationServiceLegacyTimeLocationTests.CalculateVerticalSurfaceRadiation_UsesProvidedLongitudeTimeZoneAndYear`
- `SolarRadiationServiceLegacyTimeLocationTests.CompatibilityOverloadKeepsOldSignatureButDelegatesThroughExplicitFallbackConstants`
- `SolarRadiationServiceLegacyTimeLocationTests.Iso52016LegacyFallbackCallPassesConfiguredLocationAndTimezone`
