# Engineering Core V1 Report Export Disclosure Policy

## Purpose

This policy protects Engineering Core V1 disclosures when report data is exported to user-facing formats.

The rule is simple:

Report exports must not show engineering calculation results without the related calculationDisclosure when the disclosure exists.

## Export surfaces

This policy applies to:

- frontend report UI;
- raw JSON exports;
- PDF exports;
- Excel exports;
- future report templates;
- API examples;
- generated sample reports;
- support/debug report packages.

## Required disclosure object

Every user-facing Engineering Core V1 report export should preserve:

- calculationDisclosure.coreStatus;
- calculationDisclosure.calculationScope;
- calculationDisclosure.calculationMethod;
- calculationDisclosure.actualMethod;
- calculationDisclosure.warnings;
- calculationDisclosure.assumptions;
- calculationDisclosure.explicitNonClaims;
- calculationDisclosure.outOfScopeV1;
- calculationDisclosure.documentationFiles.

## Required visible sections

PDF, Excel and frontend reports should render at least these sections:

1. Calculation scope.
2. Calculation method and actual method.
3. Warnings.
4. Assumptions.
5. Explicit non-claims.
6. Out-of-scope v1.
7. Documentation references.

## Warning visibility rule

Warnings must not be hidden only in:

- raw JSON;
- hidden worksheet cells;
- debug-only sections;
- collapsed developer-only accordions;
- browser console logs.

Warnings should be visible near the main report result.

## Non-claim visibility rule

Exports must keep these non-claims visible where relevant:

- no exact EnergyPlus numerical parity;
- no exact pyBuildingEnergy numerical parity;
- no ASHRAE 140 validation coverage;
- no full ISO 52016 node/matrix solver parity;
- no latent/moisture/humidity support in v1.

## Annual 8760 export rule

Annual energy exports may be labeled true hourly annual 8760 only when:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

If output is based on monthly adapter, synthetic weather, representative weather or fewer than 8760 hourly records, it must not be labeled as true hourly annual 8760 simulation.

## Heating export rule

Heating exports must make clear that Engineering Core V1 heating reports are engineering design-point reports.

They should disclose:

- transmission heat loss assumptions;
- ventilation/infiltration sensible heat loss assumptions;
- simplified ground or adjacent boundary assumptions when present;
- no full ISO 52016 node/matrix solver parity;
- no exact EnergyPlus or ASHRAE 140 parity.

## Cooling export rule

Cooling exports must make clear that Engineering Core V1 cooling reports are engineering design-point reports.

They should disclose:

- transmission, ventilation and infiltration assumptions;
- simplified SHGC/shading solar assumptions;
- ISO52010-inspired isotropic sky transposition;
- internal sensible gains;
- no detailed EnergyPlus solar distribution parity;
- no latent/moisture/humidity support in v1.

## Excel-specific rule

Excel exports should include a dedicated worksheet or clearly visible table named one of:

- Calculation Disclosure;
- Engineering Core Disclosure;
- Assumptions and Warnings.

The sheet/table should include warnings, assumptions, explicit non-claims, out-of-scope items and documentation references.

## PDF-specific rule

PDF exports should include a visible disclosure section before or immediately after summary totals.

The disclosure must not be placed only in tiny footer text.

## JSON-specific rule

JSON exports should preserve calculationDisclosure as structured data.

Do not flatten away warnings, assumptions or explicitNonClaims.

## Frontend-specific rule

Frontend report UI must render calculationDisclosure before raw JSON/debug output.

Current frontend component:

    src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx

## Verification

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1ReportExportDisclosureGuardTests"

Generate checklist:

    .\scripts\engineering-core\generate-engineering-core-v1-export-disclosure-checklist.ps1
