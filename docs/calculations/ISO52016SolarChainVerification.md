# ISO 52016 Solar Chain Verification

Stage 11 adds a focused verification script and evidence manifest for the completed ISO52010/Perez -> ISO52016 solar chain.

## Run

From repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-iso52016-solar-chain.ps1
```

Run all tests after the focused verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-iso52016-solar-chain.ps1 -RunAllTests
```

Skip build when CI or a previous command already built the solution:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-iso52016-solar-chain.ps1 -SkipBuild
```

## What the script verifies

The script runs the guard tests for:

- ISO52016 weather-solar context Perez diagnostics;
- component window solar gains;
- hourly heat balance solar component path;
- annual loop `weatherSolarHour` wiring;
- annual result diagnostics;
- response/API diagnostics;
- frontend/report diagnostics rendering;
- production DI Perez registration;
- production DI runtime smoke;
- API diagnostics contract evidence;
- legacy `SolarRadiationService` location/time cleanup.

## Evidence manifest

Machine-readable evidence:

- `docs/calculations/ISO52016SolarChainManifest.json`

The manifest lists:

- closed stages;
- required diagnostic codes;
- critical verification tests;
- explicit non-claims.

## Required diagnostic codes

- `Iso52016.WeatherSolarContextUsed`
- `Iso52016.SolarGainComponentPathUsed`
- `Iso52016.PerezAnisotropicModelVisibleInAnnualResult`
- `Iso52016.MatrixSolarRadiationFallbackUsed`
- `SolarWeather.PerezAnisotropicModelUsed`
- `SolarWeather.PerezSkyState`

## Explicit non-claims

This verification does not claim:

- No exact EnergyPlus numerical equivalence claim;
- No exact StandardReference numerical equivalence claim;
- No ASHRAE 140 / BESTEST-style validation anchor coverage claim;
- No full ISO 52016 node/matrix solver equivalence claim;
- No latent/moisture/humidity simulation claim.


## Stage 12 legacy cleanup guard

The focused verification script also includes:

- `SolarRadiationServiceLegacyTimeLocationTests`

This test class proves that the legacy compatibility/helper path no longer hides methodically weak solar-position inputs in configured ISO 52016 fallback calls.

The old 5-parameter method remains only as a compatibility wrapper. Configured ISO 52016 fallback calls must pass:

- `_options.LongitudeDegrees`;
- `TimeSpan.FromHours(_options.TimeZoneOffsetHours)`;
- `_options.DefaultWeatherYear`.
## Closure status

The ISO52010/Perez -> ISO52016 solar chain is considered `closed-internal-engineering-gate` when:

- all critical verification tests pass;
- `verify-iso52016-solar-chain.ps1` passes;
- `ISO52016SolarChainManifest.json` lists Stage 1 through Stage 13;
- the legacy `SolarRadiationService` cleanup guard is included in focused verification.
