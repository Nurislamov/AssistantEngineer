# ISO 52016 Solar Diagnostics End-to-End Evidence

This document records the closed chain for ISO 52016 solar-path diagnostics.

## Chain

1. Production DI registers Perez anisotropic surface irradiance by default.
2. ISO 52016 weather-solar context is built from annual climate data.
3. Annual hourly steady-state loop passes `weatherSolarHour` into the heat balance.
4. Heat balance feeds window solar gains from beam, diffuse sky and ground-reflected components.
5. Annual result exposes diagnostics.
6. Hourly/monthly response DTOs expose diagnostics.
7. Frontend disclosure panel renders diagnostics in normal report UI.
8. API consumer documentation includes a diagnostics sample.

## Required visible codes

- `Iso52016.WeatherSolarContextUsed`
- `Iso52016.SolarGainComponentPathUsed`
- `Iso52016.PerezAnisotropicModelVisibleInAnnualResult`
- `Iso52016.LegacySolarRadiationFallbackUsed`

## Guard coverage

- Runtime DI smoke verifies production path diagnostics.
- Response contract tests verify diagnostics fields exist.
- Frontend rendering tests verify diagnostics are not hidden in raw JSON only.
- API documentation tests verify the public sample includes the required diagnostic codes.

## Non-claims

This evidence does not claim EnergyPlus parity, pyBuildingEnergy parity, ASHRAE 140 coverage or full ISO 52016 node/matrix solver parity.
