# Engineering Core V1 Troubleshooting Guide

## Purpose

This guide helps diagnose common Engineering Core V1 failures.

## Frontend build fails after route changes

Symptoms:

- TypeScript error in apiRoutes.ts.
- Template string appears as ${apiPrefix}/... without backticks.

Fix:

- Ensure route values use TypeScript template literals.
- Example:

    engineeringCoreV1Status: () => `${apiPrefix}/calculations/engineering-core/v1/status`

PowerShell scripts can accidentally strip backticks when strings are not quoted safely.

## Frontend dashboard does not show Engineering Core status

Check:

- src/Frontend/src/pages/dashboard/ui/DashboardPage.tsx imports EngineeringCoreStatusPanel.
- DashboardPage renders EngineeringCoreStatusPanel.
- apiRoutes includes engineeringCoreV1Status.
- queryKeys includes engineeringCoreV1Status.
- calculationsApi exposes getEngineeringCoreV1Status.
- useEngineeringCoreStatus uses the query key and API method.

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreFrontendIntegrationGuardTests"

## Report UI does not show calculationDisclosure

Check:

- src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspace.tsx renders EngineeringCoreDisclosurePanel before raw JSON.
- backend report contract contains calculationDisclosure.
- report generator populates calculationDisclosure.
- EngineeringCoreDisclosurePanel validates warnings, assumptions, explicitNonClaims, outOfScopeV1 and documentationFiles.

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreReportDisclosureTests|EngineeringCoreFrontendIntegrationGuardTests"

## FormulaAuditMatrix test fails

Check:

- new formula has unique CalculationId;
- ClosedV1 entry includes formula, units, source principle, implementation, diagnostics, tests and limitations;
- simplified ISO/EN-inspired module includes "does not claim" wording;
- no formula item remains Partial after v1 closure.

Run:

    dotnet test .\AssistantEngineer.sln --filter "FormulaAudit"

## Annual 8760 claim looks wrong

Check:

- EnergyDataSource = TrueHourlySimulation;
- IsTrueHourly8760 = true;
- HourlyRecordCount = 8760;
- no AnnualEnergy.Not8760 warning;
- no MonthlyBalanceAdapter warning;
- no SyntheticWeather warning.

Run:

    dotnet test .\AssistantEngineer.sln --filter "AnnualEnergy8760ScenarioTests|EpwAnnualClimateDataImportServiceTests|PvgisAnnualClimateDataImportServiceTests"

## Diagnostics catalog test fails

Check:

- diagnostic codes are unique;
- severity is Error, Warning or Info;
- userMessage and userAction are not empty;
- closedV1Gate references a FormulaAuditMatrix CalculationId;
- annual 8760 warnings tell the user not to present the result as true 8760 when applicable.

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests"

## Manifest consistency fails

Check:

- docs/releases/EngineeringCoreV1Manifest.json closedFormulaGates matches FormulaAuditMatrix ClosedV1 entries;
- outOfScopeV1 matches FormulaAuditMatrix OutOfScopeV1 entries;
- plannedValidation matches FormulaAuditMatrix PlannedValidation entries;
- EngineeringCoreStatusFacade formula gate list matches manifest.

Run:

    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1

## CI fails but local fast verification passes

Run full local verification:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Check:

- Node version in CI;
- npm ci vs local npm install;
- generated or untracked frontend files;
- Windows path escaping in scripts;
- full dotnet suite, not only filtered tests.

## Do not fix by weakening claims

Do not fix tests by removing non-claims or weakening disclosure.

Required non-claims must remain visible:

- no exact EnergyPlus numerical parity;
- no exact pyBuildingEnergy numerical parity;
- no ASHRAE 140 validation coverage;
- no full ISO 52016 node/matrix solver parity;
- no latent/moisture/humidity support in v1.
