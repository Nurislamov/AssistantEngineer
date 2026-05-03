# ISO 52016 Solar Chain Verification

Stage 11 adds a focused verification script and evidence manifest for the completed ISO52010/Perez → ISO52016 solar chain.

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
- API diagnostics contract evidence.

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

- No exact EnergyPlus numerical parity claim;
- No exact pyBuildingEnergy numerical parity claim;
- No ASHRAE 140 validation coverage claim;
- No full ISO 52016 node/matrix solver parity claim;
- No latent/moisture/humidity simulation claim.

