# Engineering Core V1 Diagnostics Catalog API

## Endpoint

Diagnostics catalog endpoint:

    GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog

## Purpose

The endpoint exposes user-facing Engineering Core V1 diagnostic behavior to application and frontend consumers.

It returns:

- catalogName;
- version;
- status;
- severity rules;
- diagnostic items.

Each diagnostic item contains:

- code;
- severity;
- category;
- userMessage;
- userAction;
- closedV1Gate.

## Severity rules

| Severity | Meaning |
|---|---|
| Error | Invalid mandatory input. Calculation must fail. |
| Warning | Fallback, simplification, missing optional assumption or partial source. Calculation may succeed. |
| Info | Method, source, status or metadata. Calculation may succeed. |

A successful calculation result must not contain CalculationDiagnosticSeverity.Error.

## Important annual 8760 diagnostics

The catalog includes diagnostics that protect the annual 8760 claim:

- AnnualEnergy.Not8760;
- AnnualEnergy.SyntheticWeather;
- SolarWeather.SyntheticWeatherUsed;
- AnnualEnergy.MonthlyBalanceAdapter;
- AnnualEnergy.TrueHourlySimulationUsed.

If warning diagnostics indicate that the result is not true hourly 8760, the UI must not label it as true hourly annual simulation.

## Frontend integration

Frontend files:

    src/Frontend/src/entities/calculation/types.ts
    src/Frontend/src/entities/calculation/api/calculationsApi.ts
    src/Frontend/src/entities/calculation/model/useEngineeringCoreDiagnosticsCatalog.ts
    src/Frontend/src/shared/api/apiRoutes.ts
    src/Frontend/src/shared/api/queryKeys.ts

## Non-claims

The diagnostics catalog does not claim:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity.
