# ISO52010 / Solar Foundation Internal Engineering Gate

This note records guarded internal closure of Stage 1 solar foundation work as covered by the ISO52016 solar-chain verification lane.

## Closure status

`closed-internal-engineering-gate`

## Closed items

- `AE-ISO52010-001` - location/timezone/longitude/local solar time:
  - closed in the main annual weather-solar pipeline;
  - legacy `SolarRadiationService` configured fallback now passes longitude, timezone offset and year.
- `AE-ISO52010-002` - Perez/aniso surface irradiance calculator:
  - implemented;
  - production DI default uses `PerezAnisotropicSurfaceIrradianceCalculator`.
- `AE-SOLAR-003` - window shading benchmark fixtures / diagnostics visibility:
  - solar-chain diagnostics flow through annual result, response DTOs, API docs and frontend/report panel.

## Remaining Explicit Compatibility Note

The old 5-parameter `ISolarRadiationService.CalculateVerticalSurfaceRadiation(...)` overload remains for compatibility.

It is not the preferred annual ISO52016 solar path.

Configured ISO52016 fallback calls must use the location/time-aware overload.

## Verification

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-iso52016-solar-chain.ps1
```

Optional full suite:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-iso52016-solar-chain.ps1 -RunAllTests
```

## Explicit Non-Claims

- No exact EnergyPlus numerical equivalence claim.
- No exact StandardReference numerical equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor coverage claim.
- No full ISO 52016 node/matrix solver equivalence claim.
- No latent/moisture/humidity simulation claim.

## Compatibility Diagnostic Code Retained

- Iso52016.MatrixSolarRadiationFallbackUsed remains listed as a guarded compatibility diagnostic code for existing matrix/legacy fallback evidence.
- Its presence in the manifest is not a equivalence claim and does not make the fallback the preferred annual solar path.
