# ISO 52016 Production Runtime Smoke

Stage 9 verifies that the normal production composition root reaches the ISO 52016 weather-solar path at runtime.

## Closed scope

- `AddCalculationsModule` is used to build a real `ServiceProvider`.
- `Iso52016HourlySteadyStateCalculator` is resolved from DI.
- A full annual calculation is executed with complete hourly climate data.
- The annual result must expose:
  - `Iso52016.WeatherSolarContextUsed`;
  - `Iso52016.SolarGainComponentPathUsed`.
- The annual result must not expose:
  - `Iso52016.MatrixSolarRadiationFallbackUsed`.
- The annual result must produce non-zero `SolarGainsKWh` for a south-facing window case.

## Why this matters

Registration tests can prove that services are listed in DI, but they do not prove the runtime constructor graph actually passes `ISo52016WeatherSolarContextBuilder` into the annual calculation path.

This smoke test closes that gap.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical equivalence.
- This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- This does not remove legacy fallback support.
- This does not replace future external validation fixtures.

## Guard test

- `Iso52016ProductionSolarRuntimeSmokeTests.AddCalculationsModule_ResolvedHourlySteadyStateCalculatorUsesWeatherSolarContextPath`
