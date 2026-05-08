# ISO 52016 Production Solar Path Registration

Stage 8 closes the production DI gap for the ISO 52010/Perez -> ISO 52016 weather-solar path.

## Closed scope

- `ISurfaceIrradianceCalculator` is registered as `PerezAnisotropicSurfaceIrradianceCalculator` by default.
- `IAnnualWeatherSolarProfileBuilder` remains registered for annual weather/solar profile construction.
- `IIso52016WeatherSolarContextBuilder` remains registered for ISO 52016 weather-solar context construction.
- `Iso52016HourlySteadyStateCalculator` receives the optional weather-solar context builder through DI.
- Annual hourly ISO 52016 runs can therefore use the production weather-solar context path instead of silently falling back to legacy radiation.

## Why this matters

Previous stages added the calculation path and response/UX visibility. This stage makes the path reachable through the normal application composition root.

Without this stage, tests could prove the component path works while production DI still defaults to the older isotropic surface calculator or legacy fallback path.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical equivalence.
- This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- This does not remove legacy fallback support.
- This does not remove the old `ISolarRadiationService` design-day path.

## Guard tests

- `SolarRegistrationTests.AddCalculationsModule_RegistersSurfaceIrradianceCalculator`
- `Iso52016ProductionSolarPathRegistrationTests.AddCalculationsModule_UsesPerezAnisotropicSurfaceIrradianceByDefault`
- `Iso52016ProductionSolarPathRegistrationTests.AddCalculationsModule_RegistersWeatherSolarContextBuilderDependencies`
- `Iso52016ProductionSolarPathRegistrationTests.ProductionRegistrationDocumentsAnnualLoopWeatherSolarInjection`
