# Engineering Core V1 Public Release Notes

## Status

Engineering Core V1 is closed as an engineering formula gate with documented limitations.

This means the calculation-core scope has been implemented, documented, guarded by tests and made visible through API, reports and frontend UI.

## What is included

Engineering Core V1 includes:

- design-point heating load;
- design-point cooling load;
- transmission heat transfer;
- ventilation and infiltration sensible loads;
- internal sensible gains;
- simplified SHGC window solar gains;
- ISO52010-inspired solar position and isotropic sky surface irradiance;
- room/floor/building aggregation;
- simplified hourly heat balance;
- single thermal zone calculation path;
- EPW 8760 weather import gate;
- PVGIS 8760 weather import gate;
- annual true hourly 8760 energy integration;
- simplified ground heat transfer;
- simplified adjacent boundary handling;
- simplified DHW demand;
- simplified system final/primary energy;
- equipment capacity sizing;
- diagnostics catalog;
- report calculation disclosures;
- frontend visibility panels;
- API contract snapshots;
- OpenAPI fragment;
- report/export disclosure policy;
- validation case registry;
- traceability matrix;
- release evidence package.

## What ClosedV1 means

ClosedV1 means:

- the formula gate is implemented;
- units and scope are documented;
- diagnostics are available;
- invalid mandatory inputs fail;
- successful results must not contain Error diagnostics;
- known limitations are documented;
- report/API/frontend outputs expose assumptions and non-claims;
- CI and verification scripts protect the result.

## What ClosedV1 does not mean

ClosedV1 does not mean:

- exact EnergyPlus numerical equivalence;
- exact StandardReference numerical equivalence;
- ASHRAE 140 / BESTEST-style validation anchor coverage;
- full ISO 52016 node/matrix solver equivalence;
- full ISO 13370 implementation;
- full EN 15316 implementation;
- latent/moisture/humidity support in V1;
- detailed HVAC plant simulation.

## Annual 8760 rule

Annual energy may be presented as true hourly annual 8760 only when:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

Monthly adapters, synthetic weather and partial hourly records must not be labeled as true hourly annual 8760 simulation.

## User-visible transparency

Engineering Core V1 exposes limitations and assumptions through:

- Engineering Core status API;
- diagnostics catalog API;
- report calculationDisclosure;
- frontend status panel;
- frontend diagnostics catalog panel;
- frontend report disclosure panel;
- report/export disclosure policy.

## Verification

Use:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

For release readiness:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

## Future validation

EnergyPlus / ASHRAE 140-style validation is planned as future comparative validation with documented tolerances.

It is not part of the V1 closure claim.
