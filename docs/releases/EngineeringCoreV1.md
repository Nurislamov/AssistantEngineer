# Engineering Core V1 Release Summary

## Release status

Engineering Core V1 is closed as an engineering formula gate.

This release closes the main HVAC calculation kernel for:

- design-point heating load;
- design-point cooling load;
- transmission heat transfer;
- ventilation and infiltration sensible loads;
- internal sensible gains;
- window solar gains;
- solar position and isotropic surface irradiance;
- room/floor/building aggregation;
- single thermal zone path;
- simplified hourly heat balance;
- EPW 8760 weather import;
- PVGIS 8760 weather import;
- annual hourly 8760 kWh integration;
- simplified ground heat transfer;
- simplified adjacent boundary handling;
- simplified DHW;
- simplified system final/primary energy;
- equipment capacity sizing.

## Important limitation

Closed formula gate does not mean exact parity with external simulation tools.

Engineering Core V1 does not claim:

- exact pyBuildingEnergy numerical parity;
- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- full ISO 13370 implementation;
- full EN 15316 implementation;
- latent/moisture/humidity calculation.

## New application-visible status

Engineering Core V1 status is available through:

    GET /api/v1/calculations/engineering-core/v1/status

The endpoint exposes:

- ClosedV1 status;
- formula gates;
- explicit non-claims;
- out-of-scope v1 items;
- planned validation;
- annual 8760 requirements;
- documentation files.

## New report disclosures

Heating and cooling reports now expose:

    calculationDisclosure

Disclosure includes:

- core status;
- calculation scope;
- calculation method;
- actual method;
- warnings;
- assumptions;
- explicit non-claims;
- out-of-scope v1 items;
- documentation files.

## Validation flow change

Calculation engines must not return success with error diagnostics.

CalculationDiagnosticSeverity.Error means invalid mandatory input and must fail the calculation.

## Weather and annual energy

EPW and PVGIS imports have 8760 gates.

Annual hourly energy integration has an explicit true hourly 8760 scenario.

True hourly annual energy requires:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

## Future validation

EnergyPlus / ASHRAE 140 validation is planned as a future layer.

It will be comparative engineering validation with documented tolerances, not exact watt-by-watt parity.

## Recommended next work

After Engineering Core V1:

1. frontend diagnostics panel;
2. report templates showing assumptions/warnings/non-claims;
3. EnergyPlus smoke validation cases;
4. ASHRAE 140-style comparative case structure;
5. equipment performance curves / part-load future module;
6. latent/moisture psychrometrics future module.
