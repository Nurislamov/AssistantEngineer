# Engineering Core V1 Release Evidence

Generated at: 2026-05-02 10:12:40 UTC

## Status summary

| Field | Value |
|---|---|
| Core name | AssistantEngineer Engineering Core |
| Version | v1 |
| Status | ClosedV1 |
| Release type | engineering-formula-gate |
| Formula gates closed | True |
| Weather 8760 gates closed | True |
| Annual hourly 8760 gate closed | True |
| Success results must not contain Error diagnostics | True |

## Counts

| Item | Count |
|---|---:|
| Closed formula gates | 17 |
| Out of scope v1 items | 2 |
| Planned validation items | 1 |
| Diagnostics total | 43 |
| Error diagnostics | 24 |
| Warning diagnostics | 15 |
| Info diagnostics | 4 |

## Closed formula gates

| CalculationId | Status |
|---|---|
| HVAC.ADJACENT_ZONE.SIMPLIFIED | ClosedV1 |
| HVAC.AGGREGATION.LOAD_SUMMARY | ClosedV1 |
| HVAC.ANNUAL_ENERGY.HOURLY_KWH | ClosedV1 |
| HVAC.DHW.SIMPLIFIED | ClosedV1 |
| HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN | ClosedV1 |
| HVAC.GROUND.SIMPLIFIED | ClosedV1 |
| HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC | ClosedV1 |
| HVAC.INTERNAL_GAINS.SENSIBLE | ClosedV1 |
| HVAC.ROOM_LOAD.DESIGN_POINT | ClosedV1 |
| HVAC.SOLAR.SURFACE_IRRADIANCE_ISOTROPIC | ClosedV1 |
| HVAC.SYSTEM_ENERGY.SIMPLIFIED | ClosedV1 |
| HVAC.THERMAL_ZONE.SINGLE_ZONE | ClosedV1 |
| HVAC.TRANSMISSION.SIMPLE_UA | ClosedV1 |
| HVAC.VENTILATION.SENSIBLE_AIRFLOW | ClosedV1 |
| HVAC.WINDOW_SOLAR.SIMPLE_SHGC | ClosedV1 |
| WEATHER.EPW_8760 | ClosedV1 |
| WEATHER.PVGIS_8760 | ClosedV1 |

## Annual 8760 requirements

True hourly annual energy requires:

- EnergyDataSource = TrueHourlySimulation
- IsTrueHourly8760 = true
- HourlyRecordCount = 8760

## Application endpoints

- GET /api/v1/calculations/engineering-core/v1/status

## Frontend visibility files

- src/Frontend/src/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel.tsx
- src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx
- src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspace.tsx
- src/Frontend/src/pages/dashboard/ui/DashboardPage.tsx

## Backend visibility files

- src/Backend/AssistantEngineer.Modules.Calculations/Application/Facades/EngineeringCoreStatusFacade.cs
- src/Backend/AssistantEngineer.Api/Controllers/Calculations/EngineeringCoreStatusController.cs
- src/Backend/AssistantEngineer.Modules.Reporting/Application/Contracts/Reports/Common/CalculationDisclosure.cs

## Verification scripts

- scripts/engineering-core/verify-engineering-core-v1.ps1
- scripts/engineering-core/verify-engineering-core-v1-manifest.ps1

## CI workflows

- .github/workflows/engineering-core-v1.yml

## Out of scope v1

- HVAC.LATENT_LOAD
- HVAC.MOISTURE_BALANCE

## Planned validation

- VALIDATION.ENERGYPLUS_ASHRAE140

## Explicit non-claims

- No exact pyBuildingEnergy numerical parity claim.
- No exact EnergyPlus numerical parity claim.
- No ASHRAE 140 validation coverage claim.
- No full ISO 52016 node/matrix solver parity claim.
- No full ISO 52010 climate conversion parity claim.
- No full ISO 13370 implementation claim.
- No full EN 15316 generation/distribution/storage/emission chain claim.
- No full coupled multi-zone heat-balance simulation claim.
- No latent/moisture/humidity calculation claim.

## Diagnostics by category

| Category | Count |
|---|---:|
| Aggregation | 3 |
| AnnualEnergy | 13 |
| EquipmentSizing | 12 |
| SystemEnergy | 13 |
| Transmission | 1 |
| Weather | 1 |

## Diagnostics catalog

| Code | Severity | Category | ClosedV1 gate |
|---|---|---|---|
| Aggregation.HourlyUnavailable | Warning | Aggregation | HVAC.AGGREGATION.LOAD_SUMMARY |
| Aggregation.InvalidRoomArea | Error | Aggregation | HVAC.AGGREGATION.LOAD_SUMMARY |
| Aggregation.NoRooms | Warning | Aggregation | HVAC.AGGREGATION.LOAD_SUMMARY |
| AnnualEnergy.InvalidArea | Error | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.InvalidBuildingId | Error | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.InvalidHourDuration | Error | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.InvalidMonth | Error | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.MonthlyBalanceAdapter | Warning | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.NegativeHourlyValueClamped | Warning | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.NoHourlyInputs | Error | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.Not8760 | Warning | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.SignedComponentBalanceAvailable | Info | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.SourceUnavailable | Error | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.SyntheticWeather | Warning | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.TrueHourlySimulationPartial | Warning | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| AnnualEnergy.TrueHourlySimulationUsed | Info | AnnualEnergy | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| EquipmentSizing.CoolingSafetyFactorApplied | Info | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.DefaultCoolingSafetyFactorUsed | Warning | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.DefaultHeatingSafetyFactorUsed | Warning | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.HeatingSafetyFactorApplied | Info | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.InvalidCoolingLoad | Error | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.InvalidCoolingSafetyFactor | Error | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.InvalidHeatingLoad | Error | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.InvalidHeatingSafetyFactor | Error | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.InvalidSafetyFactor | Error | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.InvalidTargetId | Error | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.NoEquipmentFound | Warning | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| EquipmentSizing.NoRecommendedEquipment | Warning | EquipmentSizing | HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN |
| SolarWeather.SyntheticWeatherUsed | Warning | Weather | HVAC.ANNUAL_ENERGY.HOURLY_KWH |
| SystemEnergy.CoolingAssumptionMissing | Warning | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.DhwAssumptionMissing | Warning | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.HeatingAssumptionMissing | Warning | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidCoolingCop | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidDhwCop | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidDhwEfficiency | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidFanEnergy | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidHeatingCop | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidHeatingEfficiency | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidPrimaryEnergyFactor | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidUsefulCooling | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidUsefulDhw | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| SystemEnergy.InvalidUsefulHeating | Error | SystemEnergy | HVAC.SYSTEM_ENERGY.SIMPLIFIED |
| Transmission.MissingBoundaryTemperature | Error | Transmission | HVAC.ADJACENT_ZONE.SIMPLIFIED |

## Documentation inventory

| File | Status |
|---|---|
| docs/calculations/EnergyPlusAshrae140ValidationPlan.md | present |
| docs/calculations/EngineeringCoreV1ApiExamples.md | present |
| docs/calculations/EngineeringCoreV1DeveloperGuide.md | present |
| docs/calculations/EngineeringCoreV1ReleaseNotes.md | present |
| docs/calculations/EngineeringCoreV1Scope.md | present |
| docs/calculations/EngineeringCoreV1VerificationRunbook.md | present |
| docs/ci/EngineeringCoreV1CI.md | present |
| docs/contributing/EngineeringCoreV1ContributionGuide.md | present |
| docs/frontend/EngineeringCoreV1FrontendIntegrationGuard.md | present |
| docs/frontend/EngineeringCoreV1ReportDisclosurePanel.md | present |
| docs/frontend/EngineeringCoreV1StatusPanel.md | present |
| docs/releases/EngineeringCoreV1.md | present |
| docs/releases/EngineeringCoreV1Manifest.json | present |
| docs/releases/EngineeringCoreV1OwnerHandoff.md | present |
| docs/releases/EngineeringCoreV1ReleaseChecklist.md | present |
| docs/releases/EngineeringCoreV1ReleaseManifest.md | present |
| docs/validation/EnergyPlusAshrae140ValidationHarness.md | present |
| docs/validation/EnergyPlusValidationCaseTemplate.md | present |

## Required verification command

Full verification:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Fast verification:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

Manifest verification:

    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1

## Release interpretation

Engineering Core V1 is closed as an engineering formula gate.

This release evidence does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity, ASHRAE 140 validation coverage, full ISO 52016 node/matrix solver parity, full ISO 13370 implementation, full EN 15316 implementation or latent/moisture/humidity support in v1.
