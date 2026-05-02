# Engineering Core V1 Traceability Matrix

## Status

| Field | Value |
|---|---|
| Matrix name | Engineering Core V1 Traceability Matrix |
| Version | v1 |
| Status | ClosedV1 |
| Closed formula gates | 17 |
| Diagnostics | 43 |
| Validation cases | 5 |

## Sources

- docs/releases/EngineeringCoreV1Manifest.json
- docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json
- docs/validation/EnergyPlusValidationCaseRegistry.json

## Annual 8760 requirements

- EnergyDataSource = TrueHourlySimulation
- IsTrueHourly8760 = true
- HourlyRecordCount = 8760

## Application endpoints

- GET /api/v1/calculations/engineering-core/v1/status

## Closed formula gates

| CalculationId | Status | Diagnostics | API | Report disclosure | Frontend |
|---|---|---:|---|---|---|
| HVAC.ADJACENT_ZONE.SIMPLIFIED | ClosedV1 | 1 | True | True | True |
| HVAC.AGGREGATION.LOAD_SUMMARY | ClosedV1 | 3 | True | True | True |
| HVAC.ANNUAL_ENERGY.HOURLY_KWH | ClosedV1 | 14 | True | True | True |
| HVAC.DHW.SIMPLIFIED | ClosedV1 | 0 | True | True | True |
| HVAC.EQUIPMENT_SIZING.CAPACITY_MARGIN | ClosedV1 | 12 | True | True | True |
| HVAC.GROUND.SIMPLIFIED | ClosedV1 | 0 | True | True | True |
| HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC | ClosedV1 | 0 | True | True | True |
| HVAC.INTERNAL_GAINS.SENSIBLE | ClosedV1 | 0 | True | True | True |
| HVAC.ROOM_LOAD.DESIGN_POINT | ClosedV1 | 0 | True | True | True |
| HVAC.SOLAR.SURFACE_IRRADIANCE_ISOTROPIC | ClosedV1 | 0 | True | True | True |
| HVAC.SYSTEM_ENERGY.SIMPLIFIED | ClosedV1 | 13 | True | True | True |
| HVAC.THERMAL_ZONE.SINGLE_ZONE | ClosedV1 | 0 | True | True | True |
| HVAC.TRANSMISSION.SIMPLE_UA | ClosedV1 | 0 | True | True | True |
| HVAC.VENTILATION.SENSIBLE_AIRFLOW | ClosedV1 | 0 | True | True | True |
| HVAC.WINDOW_SOLAR.SIMPLE_SHGC | ClosedV1 | 0 | True | True | True |
| WEATHER.EPW_8760 | ClosedV1 | 0 | True | True | True |
| WEATHER.PVGIS_8760 | ClosedV1 | 0 | True | True | True |

## Validation cases

| CaseId | Stage | Status | Metrics |
|---|---|---|---:|
| EP-SMOKE-001 | Smoke | ReferenceFixturePlaceholder | 3 |
| EP-SMOKE-002 | Smoke | ReferenceFixturePlaceholder | 3 |
| EP-SMOKE-003 | Smoke | ReferenceFixturePlaceholder | 3 |
| ASHRAE140-STYLE-001 | Ashrae140Style | Planned | 2 |
| ASHRAE140-STYLE-002 | Ashrae140Style | Planned | 2 |

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

## Verification scripts

- scripts/engineering-core/verify-engineering-core-v1.ps1
- scripts/engineering-core/verify-engineering-core-v1-manifest.ps1

## CI workflows

- .github/workflows/engineering-core-v1.yml

## Interpretation

This matrix proves traceability between the closed Engineering Core V1 formula gates, diagnostics catalog, validation registry, API visibility, report/frontend visibility, documentation, verification scripts and CI workflow.

It does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity, ASHRAE 140 validation coverage, full ISO 52016 node/matrix solver parity or latent/moisture/humidity support in v1.
