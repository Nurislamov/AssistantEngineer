# External Reference Validation Plan

AssistantEngineer is a standalone C# standard-based calculation platform.
External tools are used only as independent comparison workflows.
No full validation, exact-match, or compliance claim is made without fixed fixtures, expected outputs, tolerances, and CI gates.

## Naming Rule

Use neutral naming in product code, tests, and public metadata:

- `ExternalReferenceValidation`
- `StandardReferenceFixture`
- `ReferenceCaseInputAdapter`
- `ExternalComparisonWorkflow`

Avoid legacy parity-style naming in product and public assets.

## Claim Boundary

AssistantEngineer implements calculation logic directly in C# using standard-based engineering methods.
This scope is internal engineering and comparison-workflow ready; it is not a certification-grade validation statement.

## P0 - Core Calculation Scope

| Code | Feature | Current status |
|---|---|---|
| STANDARD_REFERENCE.TRANSMISSION_HEAT_TRANSFER | Transmission heat transfer | InternalDeterministicTested |
| STANDARD_REFERENCE.WINDOW_SOLAR_GAINS | Window solar gains | BenchmarkCompared |
| STANDARD_REFERENCE.VENTILATION_INFILTRATION_LOADS | Ventilation and infiltration loads | InternalDeterministicTested |
| STANDARD_REFERENCE.INTERNAL_GAINS | Internal gains | InternalDeterministicTested |
| STANDARD_REFERENCE.ROOM_HEATING_LOAD | Room heating load | InternalDeterministicTested |
| STANDARD_REFERENCE.ROOM_COOLING_LOAD | Room cooling load | InternalDeterministicTested |
| STANDARD_REFERENCE.THERMAL_ZONE_AGGREGATION | Thermal zone aggregation | InternalDeterministicTested |
| STANDARD_REFERENCE.FLOOR_AGGREGATION | Floor aggregation | InternalDeterministicTested |
| STANDARD_REFERENCE.BUILDING_AGGREGATION | Building aggregation | InternalDeterministicTested |
| STANDARD_REFERENCE.ANNUAL_ENERGY_BALANCE | Annual energy balance | BenchmarkCompared |
| STANDARD_REFERENCE.SIGNED_COMPONENT_BALANCE | Signed component balance | BenchmarkCompared |
| STANDARD_REFERENCE.DHW_DEMAND | DHW demand | InternalDeterministicTested |
| STANDARD_REFERENCE.SYSTEM_ENERGY | System energy | InternalDeterministicTested |
| STANDARD_REFERENCE.EQUIPMENT_SIZING_INTEGRATION | Equipment sizing integration | InternalDeterministicTested |

## P1 - Scope Expansion

| Code | Feature | Current status |
|---|---|---|
| WEATHER.PVGIS | PVGIS weather input normalization | Partial |
| ISO52016.MULTI_ZONE | Multi-zone calculation | Partial |
| ISO52016.ADJACENT_HEATED_ZONE | Adjacent heated zones / adiabatic walls | Partial |
| ISO52016.ADJACENT_NON_HEATED_ZONE | Adjacent non-heated zones | Partial |
| DHW.EN12831_3 | Domestic hot water volume and energy need | Partial |
| PRIMARY_ENERGY.EN15316_1 | Primary energy calculation | Partial |

## P3 - Out of Scope

| Code | Feature | Reason |
|---|---|---|
| LATENT.ENERGY_NEED | Latent energy need | Not in current validation scope |
| LATENT.MOISTURE_LOAD | Moisture/latent load | Not in current validation scope |
| SUPPLY_AIR.HUMIDIFICATION_CONDITIONS | Supply-air humidification/dehumidification conditions | Not in current validation scope |

## Real Application Pipeline

- Public method query values are preserved as `requestedMethod`.
- Results expose `actualMethod` and diagnostics when API compatibility routes use the Standard reference design-point calculation pipeline.
- Room, floor, building, annual, DHW, and system-energy routes remain deterministic and traceable through application services.
- Comparison workflows and anchors are explicit and non-claiming.

## Implementation Order

1. Validation matrix and guard tests.
2. ISO 52010 weather/solar layer.
3. ISO 52016 hourly core.
4. Monthly and annual aggregation.
5. Multi-zone and adjacent boundaries.
6. DHW calculation.
7. Primary energy.
8. API/reporting integration.

## Fixture Policy

Each fixture should include input, expected outputs (hourly/monthly/annual where relevant), tolerance, and assumptions.

## Tolerance Policy

Baseline defaults:

- hourly temperature: +/-0.05 C
- hourly load: +/-1 W
- monthly demand: +/-0.01 kWh
- annual demand: +/-0.1 kWh
