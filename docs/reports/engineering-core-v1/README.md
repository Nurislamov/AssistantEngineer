# Engineering Core V1 Report Contract Snapshots

## Purpose

This folder contains sample report JSON snapshots for Engineering Core V1 report consumers.

The snapshots demonstrate how calculationDisclosure must appear in user-visible report output.

## Files

- docs/reports/engineering-core-v1/heating-report.sample.json
- docs/reports/engineering-core-v1/cooling-report.sample.json
- docs/reports/engineering-core-v1/annual-energy-disclosure.sample.json

## Generation

Regenerate snapshots:

    .\scripts\engineering-core\generate-engineering-core-v1-report-contract-snapshots.ps1

## Required disclosure fields

Every Engineering Core V1 user-visible report disclosure must include:

- coreStatus;
- calculationScope;
- calculationMethod;
- actualMethod;
- warnings;
- assumptions;
- explicitNonClaims;
- outOfScopeV1;
- documentationFiles.

## Report requirements

Heating report disclosure must state:

- Engineering-core v1 heating design-point report;
- transmission and ventilation/infiltration assumptions;
- no full ISO 52016 node/matrix solver equivalence;
- no exact EnergyPlus, ASHRAE 140 or StandardReference numerical equivalence;
- latent/moisture/detailed psychrometrics out of scope.

Cooling report disclosure must state:

- Engineering-core v1 cooling design-point report;
- transmission, ventilation, infiltration, solar and internal gain assumptions;
- simplified SHGC/shading solar model;
- isotropic sky transposition;
- no detailed EnergyPlus solar distribution equivalence.

Annual energy disclosure must state:

- true hourly 8760 is valid only when EnergyDataSource=TrueHourlySimulation, IsTrueHourly8760=true and HourlyRecordCount=8760;
- monthly adapter, synthetic weather and deterministic short fixtures must not be presented as true hourly 8760 annual simulation.

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ReportContractSnapshotTests"

## Non-claims

Report consumers must keep these visible:

- no exact EnergyPlus numerical equivalence;
- no exact StandardReference numerical equivalence;
- no ASHRAE 140 / BESTEST-style validation anchor coverage;
- no full ISO 52016 node/matrix solver equivalence;
- no latent/moisture/humidity support in v1.
