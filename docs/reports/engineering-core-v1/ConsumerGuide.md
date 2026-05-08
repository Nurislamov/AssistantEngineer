# Engineering Core V1 Report Consumer Guide

## Purpose

This guide explains how frontend, export and reporting consumers should interpret Engineering Core V1 report output.

## Main rule

Report results must not be shown without their calculationDisclosure when the disclosure is available.

calculationDisclosure explains:

- core status;
- calculation scope;
- calculation method;
- actual method;
- warnings;
- assumptions;
- explicit non-claims;
- out-of-scope v1 items;
- documentation files.

## Heating reports

Heating reports are engineering design-point reports.

They may include:

- transmission heat loss;
- ventilation/infiltration sensible heat loss;
- room-level heating load;
- building-level heating load;
- calculationDisclosure.

They do not claim:

- full ISO 52016 node/matrix solver equivalence;
- exact EnergyPlus numerical equivalence;
- exact StandardReference numerical equivalence;
- ASHRAE 140 / BESTEST-style validation anchor coverage;
- latent/moisture/humidity support.

## Cooling reports

Cooling reports are engineering design-point reports.

They may include:

- transmission load;
- ventilation/infiltration load;
- solar gains;
- internal gains;
- room/floor/building cooling load;
- equipment capacity margin data;
- calculationDisclosure.

They do not claim:

- detailed EnergyPlus solar distribution equivalence;
- full optical glazing model equivalence;
- full ISO 52016 node/matrix solver equivalence;
- latent/moisture/humidity support.

## Annual energy reports

Annual energy reports may be true hourly annual 8760 only when:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

If a report is based on monthly adapter, synthetic weather or fewer than 8760 hourly records, it must not be labeled as true hourly annual 8760.

## Export rule

PDF, Excel, JSON and frontend exports should include:

- calculationDisclosure.warnings;
- calculationDisclosure.assumptions;
- calculationDisclosure.explicitNonClaims;
- calculationDisclosure.outOfScopeV1;
- calculationDisclosure.documentationFiles.

Warnings and non-claims must not be removed from exports.

## Sample snapshots

See:

- docs/reports/engineering-core-v1/heating-report.sample.json
- docs/reports/engineering-core-v1/cooling-report.sample.json
- docs/reports/engineering-core-v1/annual-energy-disclosure.sample.json
