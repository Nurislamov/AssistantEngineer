# Engineering Core V1 API Consumer Guide

## Purpose

This guide explains how external or frontend consumers should use the Engineering Core V1 API contract.

Engineering Core V1 is closed as an engineering formula gate. It is not an exact external-simulator equivalence contract.

## Endpoints

Status:

    GET /api/v1/calculations/engineering-core/v1/status

Diagnostics catalog:

    GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog

## Contract files

- docs/api/engineering-core-v1/openapi.fragment.yml
- docs/api/engineering-core-v1/postman_collection.json
- docs/api/engineering-core-v1/status.sample.json
- docs/api/engineering-core-v1/diagnostics-catalog.sample.json
- docs/api/engineering-core-v1/engineering-core-v1.http

## Status endpoint usage

Use the status endpoint to show:

- ClosedV1 status;
- formula gate count;
- annual 8760 requirements;
- explicit non-claims;
- out-of-scope v1 items;
- planned validation;
- documentation links.

Consumers must not interpret ClosedV1 as exact EnergyPlus, StandardReference or ASHRAE 140 equivalence.

## Diagnostics catalog usage

Use the diagnostics catalog to render user-facing messages.

Severity behavior:

| Severity | UI behavior |
|---|---|
| Error | Blocking issue; calculation must fail. |
| Warning | Visible warning near result; calculation may succeed. |
| Info | Metadata/details; calculation may succeed. |

Every diagnostic has:

- code;
- severity;
- category;
- userMessage;
- userAction;
- closedV1Gate.

## Annual 8760 UI rule

A result may be presented as true hourly annual 8760 only when:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

If diagnostics include AnnualEnergy.Not8760, AnnualEnergy.MonthlyBalanceAdapter, AnnualEnergy.SyntheticWeather or SolarWeather.SyntheticWeatherUsed, the UI must not label the result as true hourly annual 8760.

## Required non-claims

Consumers must keep these visible where relevant:

- no exact EnergyPlus numerical equivalence;
- no exact StandardReference numerical equivalence;
- no ASHRAE 140 / BESTEST-style validation anchor coverage;
- no full ISO 52016 node/matrix solver equivalence;
- no latent/moisture/humidity support in v1.

## Compatibility rule

Adding fields is allowed when backward-compatible.

Removing or renaming these fields is a breaking contract change:

- status;
- formulaGatesClosed;
- weather8760GatesClosed;
- annualHourly8760GateClosed;
- successfulResultsMustNotContainErrorDiagnostics;
- formulaGates;
- explicitNonClaims;
- outOfScopeV1;
- plannedValidation;
- requiredAnnual8760Flags;
- documentationFiles;
- rules;
- diagnostics;
- code;
- severity;
- category;
- userMessage;
- userAction;
- closedV1Gate.

Breaking changes require updating:

- OpenAPI fragment;
- sample JSON snapshots;
- Postman collection;
- frontend types;
- frontend guards;
- API contract tests;
- release manifest if claims change.
