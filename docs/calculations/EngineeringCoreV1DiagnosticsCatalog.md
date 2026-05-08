# Engineering Core V1 Diagnostics Catalog

## Purpose

This catalog defines user-facing behavior for Engineering Core V1 diagnostics.

Structured catalog:

    docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json

The catalog supports:

- consistent UI rendering;
- clear user actions;
- Error/Warning/Info severity rules;
- traceability to ClosedV1 formula gates;
- stable diagnostics documentation.

## Severity rules

| Severity | Meaning | Calculation result |
|---|---|---|
| Error | Invalid mandatory input. | Calculation must fail. |
| Warning | Fallback, simplification, missing optional assumption or partial source. | Calculation may succeed. |
| Info | Method, source, status or metadata. | Calculation may succeed. |

A successful calculation result must not contain CalculationDiagnosticSeverity.Error.

## UX requirements

Frontend and reports should display diagnostics as follows:

| Severity | UI behavior |
|---|---|
| Error | Blocking error banner, action required. |
| Warning | Visible warning panel near results. |
| Info | Expandable metadata/details section. |

Warnings must not be hidden in raw JSON or debug-only views.

## Required diagnostic metadata

Every catalog item includes:

- code;
- severity;
- category;
- userMessage;
- userAction;
- closedV1Gate.

## Important annual energy diagnostics

Annual energy diagnostics protect the 8760 claim.

True hourly annual reporting requires:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

Important warnings:

- AnnualEnergy.Not8760;
- AnnualEnergy.SyntheticWeather;
- SolarWeather.SyntheticWeatherUsed;
- AnnualEnergy.MonthlyBalanceAdapter;
- AnnualEnergy.TrueHourlySimulationPartial.

These warnings mean the result must not be presented as true hourly 8760 annual simulation.

## Important simplified model diagnostics

Simplified or fallback behavior should be visible as Warning or Info.

Examples:

- SystemEnergy.HeatingAssumptionMissing;
- SystemEnergy.CoolingAssumptionMissing;
- SystemEnergy.DhwAssumptionMissing;
- EquipmentSizing.DefaultHeatingSafetyFactorUsed;
- EquipmentSizing.DefaultCoolingSafetyFactorUsed;
- Aggregation.HourlyUnavailable.

## Blocking diagnostics

Blocking diagnostics include invalid mandatory inputs.

Examples:

- AnnualEnergy.InvalidArea;
- AnnualEnergy.InvalidHourDuration;
- AnnualEnergy.InvalidMonth;
- SystemEnergy.InvalidCoolingCop;
- EquipmentSizing.InvalidSafetyFactor;
- Aggregation.InvalidRoomArea;
- Transmission.MissingBoundaryTemperature.

## Non-claims

Diagnostics do not claim:

- exact EnergyPlus numerical equivalence;
- exact StandardReference numerical equivalence;
- ASHRAE 140 / BESTEST-style validation anchor coverage;
- full ISO 52016 node/matrix solver equivalence;
- full ISO 13370 implementation;
- latent/moisture/humidity support in v1.
