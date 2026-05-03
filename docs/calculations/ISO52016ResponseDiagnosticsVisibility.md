# ISO 52016 Response Diagnostics Visibility

Stage 6 propagates annual ISO 52016 diagnostics to response-level contracts.

## Closed scope

- `Iso52016HourlyResultsResponse` carries diagnostics.
- `Iso52016MonthlyResultsResponse` carries diagnostics.
- `BuildingPerformanceService` maps `Iso52016AnnualEnergyNeedResult.Diagnostics` into hourly and monthly responses.
- API-facing response contracts can now disclose whether the calculation used:
  - `Iso52016.WeatherSolarContextUsed`;
  - `Iso52016.SolarGainComponentPathUsed`;
  - `Iso52016.PerezAnisotropicModelVisibleInAnnualResult`;
  - `Iso52016.LegacySolarRadiationFallbackUsed`.

## Explicit non-claims

- This does not claim exact EnergyPlus numerical parity.
- This does not claim ASHRAE 140 validation coverage.
- This does not remove legacy fallback support.
- This does not add frontend rendering yet.

## Guard tests

- `Iso52016ResponseDiagnosticsVisibilityTests.HourlyResultsResponseCarriesDiagnostics`
- `Iso52016ResponseDiagnosticsVisibilityTests.MonthlyResultsResponseCarriesDiagnostics`
- `Iso52016ResponseDiagnosticsVisibilityTests.BuildingPerformanceServiceMapsAnnualDiagnosticsToIso52016Responses`
