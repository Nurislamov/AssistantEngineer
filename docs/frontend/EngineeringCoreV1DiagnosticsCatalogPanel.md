# Engineering Core V1 Diagnostics Catalog Panel

## Purpose

The frontend dashboard displays the Engineering Core V1 diagnostics catalog so users and support engineers can see Error / Warning / Info rules without reading raw JSON.

Component:

    src/Frontend/src/widgets/engineering-core-diagnostics-catalog/ui/EngineeringCoreDiagnosticsCatalogPanel.tsx

## Data source

The panel uses:

    GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog

Frontend hook:

    src/Frontend/src/entities/calculation/model/useEngineeringCoreDiagnosticsCatalog.ts

## Displayed information

The panel displays:

- catalog status;
- Error / Warning / Info counts;
- severity rules;
- success rule;
- annual 8760 safeguard diagnostics;
- blocking Error diagnostics;
- Warning diagnostics and user actions;
- closedV1Gate mapping for each shown diagnostic.

## Required annual 8760 safeguards

The panel must keep these diagnostics visible when present:

- AnnualEnergy.Not8760;
- AnnualEnergy.SyntheticWeather;
- SolarWeather.SyntheticWeatherUsed;
- AnnualEnergy.MonthlyBalanceAdapter;
- AnnualEnergy.TrueHourlySimulationPartial.

These diagnostics prevent adapted, synthetic or partial results from being presented as true hourly annual 8760 simulation.

## UX rule

Error diagnostics are blocking.

Warning diagnostics must stay visible near user results, reports or support panels.

Info diagnostics can be shown as metadata.

Warnings must not be hidden behind raw JSON, browser console or debug-only UI.
