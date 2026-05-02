# Engineering Core V1 Release Manifest

## Status

Engineering Core V1 is closed as an engineering formula gate.

Structured manifest:

    docs/releases/EngineeringCoreV1Manifest.json

The JSON manifest is the handoff index for:

- closed formula gates;
- out-of-scope v1 items;
- planned validation items;
- annual 8760 requirements;
- backend status endpoint;
- frontend visibility components;
- report disclosure contracts;
- verification scripts;
- CI workflow;
- release documentation.

## Closed formula gates

Engineering Core V1 closes these formula gates:

- HVAC.TRANSMISSION.SIMPLE_UA
- HVAC.VENTILATION.SENSIBLE_AIRFLOW
- HVAC.INTERNAL_GAINS.SENSIBLE
- HVAC.WINDOW_SOLAR.SIMPLE_SHGC
- HVAC.SOLAR.SURFACE_IRRADIANCE_ISOTROPIC
- HVAC.ROOM_LOAD.DESIGN_POINT
- HVAC.AGGREGATION.LOAD_SUMMARY
- HVAC.ANNUAL_ENERGY.HOURLY_KWH
- WEATHER.EPW_8760
- WEATHER.PVGIS_8760
- HVAC.DHW.SIMPLIFIED
- HVAC.SYSTEM_ENERGY.SIMPLIFIED
- HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN
- HVAC.GROUND.SIMPLIFIED
- HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC
- HVAC.THERMAL_ZONE.SINGLE_ZONE
- HVAC.ADJACENT_ZONE.SIMPLIFIED

## Out of scope v1

- HVAC.LATENT_LOAD
- HVAC.MOISTURE_BALANCE

## Planned validation

- VALIDATION.ENERGYPLUS_ASHRAE140

EnergyPlus / ASHRAE 140 validation remains planned comparative validation. It is not a v1 parity claim.

## Annual 8760 requirements

True hourly annual energy requires:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

## Application visibility

Status endpoint:

    GET /api/v1/calculations/engineering-core/v1/status

Backend visibility:

    src/Backend/AssistantEngineer.Modules.Calculations/Application/Facades/EngineeringCoreStatusFacade.cs
    src/Backend/AssistantEngineer.Api/Controllers/Calculations/EngineeringCoreStatusController.cs
    src/Backend/AssistantEngineer.Modules.Reporting/Application/Contracts/Reports/Common/CalculationDisclosure.cs

Frontend visibility:

    src/Frontend/src/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel.tsx
    src/Frontend/src/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel.tsx
    src/Frontend/src/widgets/building-workspace/ui/BuildingWorkspace.tsx
    src/Frontend/src/pages/dashboard/ui/DashboardPage.tsx

## Verification

Full verification:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Fast verification:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -Fast

Manifest verification:

    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1

## Non-claims

This release does not claim:

- exact pyBuildingEnergy numerical parity;
- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- full ISO 52010 climate conversion parity;
- full ISO 13370 implementation;
- full EN 15316 generation/distribution/storage/emission chain;
- full coupled multi-zone heat-balance simulation;
- latent/moisture/humidity calculation.
