# Energy Calculation Parity Verification

Verification is based on internal deterministic fixtures, focused engine tests, and application pipeline tests where the real backend path has been integrated.

## Current Status

| Function | Status | Evidence |
| --- | --- | --- |
| Transmission Heat Transfer | InternalDeterministicTested | Engine tests, transmission fixtures, room load application pipeline integration |
| Window Solar Gains | InternalDeterministicTested | Engine tests, solar fixtures, room cooling application pipeline integration, annual-climate source/fallback diagnostics |
| Ventilation and Infiltration Loads | InternalDeterministicTested | Engine tests, ventilation fixtures, room load application pipeline integration, default ACH diagnostics |
| Internal Gains | InternalDeterministicTested | Engine tests, room cooling application pipeline integration, design-point schedule assumption diagnostics |
| Room Heating Load | InternalDeterministicTested | Room load engine tests, fixtures, application pipeline tests, endpoint facade integration, requested/actual method diagnostics |
| Room Cooling Load | InternalDeterministicTested | Room load engine tests, fixtures, application pipeline tests, endpoint facade integration, requested/actual method diagnostics |
| Thermal Zone Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, application pipeline tests |
| Floor Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, floor application pipeline tests |
| Building Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, building application pipeline tests |
| Annual Energy Balance | InternalDeterministicTested | Annual engine tests, fixtures, application pipeline adapter tests, hourly component mapper tests, report facade integration, monthly adapter/hourly source diagnostics |
| DHW Demand | InternalDeterministicTested | DHW deterministic service tests, fixtures, endpoint facade path |
| System Energy | InternalDeterministicTested | System energy tests, fixtures, heating/cooling system services call SystemEnergyEngine |
| Equipment Sizing Integration | InternalDeterministicTested | Equipment sizing tests, fixtures, room equipment application pipeline tests, endpoint/report integration, heating capacity diagnostics |

No function is marked ExternalParityCovered in this pass.

## Real Application Pipeline

The real backend calculation path now uses `EnergyCalculationPipelineService` behind `ILoadCalculationsFacade`.

- Room heating and cooling endpoints assemble room, envelope, ventilation, infiltration, ground, solar and internal-gain inputs, then call `RoomLoadCalculationEngine`. `requestedMethod`, `actualMethod` and compatibility warnings are exposed where the public API method differs from the actual Energy Calculation Parity design-point pipeline.
- Floor and building load endpoints consume room load results and call `LoadAggregationEngine` in design-point mode. Diagnostics identify the design-point aggregation mode when hourly source data is not available.
- Building energy balance uses the existing building energy source as an explicit adapter and feeds `AnnualEnergyBalanceEngine`. Diagnostics expose `TrueHourlySimulation` versus `MonthlyBalanceAdapter`, `hourlyRecordCount`, and `isTrueHourly8760`; representative monthly records are always false. True hourly source records now pass available transmission, combined ventilation, ground, solar and internal-gain components through to the annual engine; infiltration remains partial when not separately modelled.
- DHW remains on the deterministic `DomesticHotWaterDemandService` facade path.
- Heating and cooling system services call `SystemEnergyEngine`, preserving useful, final and primary energy as separate values.
- Room equipment selection uses the actual room load, separate project heating/cooling safety factors, `EquipmentSizingEngine`, and the active equipment catalog provider. Heating capacity is evaluated when catalog candidates expose it; otherwise diagnostics state that heating sizing is skipped. Empty catalogs and rejected candidates return diagnostics instead of silent selections.
- Cooling, heating and energy-balance reports consume facade results built from the same application pipeline. Excel generation stays in Infrastructure integrations.
- Benchmark verification obtains AssistantEngineer cooling results through `ILoadCalculationsFacade`, so the benchmark comparison path uses the same application pipeline result as the public load-calculation route. This is still only benchmark evidence when a comparison test actually runs and passes.

## Regression Rules

- Non-pending fixtures must load.
- Deterministic fixtures must pass within tolerance.
- Annual totals must equal monthly sums within tolerance.
- Design building loads must equal sums of unique room loads in design-point mode.
- Diagnostics must exist for fallback or clamped assumptions.
- Diagnostics must identify requested method versus actual method for compatibility requests.
- Energy-balance diagnostics must identify `MonthlyBalanceAdapter` versus `TrueHourlySimulation` and must not over-warn for hourly components that are present.
- Room cooling separates solar, internal, transmission and ventilation components.
- Room heating separates transmission, ventilation, infiltration and ground components.
- Equipment sizing explains rejected candidates.

## Known Limits

The current status proves internal deterministic consistency and real application pipeline integration for the listed backend paths. The load-calculations annual endpoint is not a full 8760 simulation unless the upstream source supplies 8760 hourly records and the result says `energyDataSource = TrueHourlySimulation`. The true hourly component breakdown still reports infiltration as partial when it is not separately modelled. The design-point room load path is not full ISO hourly balance. No status in this matrix proves external benchmark parity.
START SECTION

## Signed hourly component balance

The true hourly simulation path now supports signed component balance fields for available hourly components.

The existing magnitude fields remain available:

- TransmissionW
- VentilationW
- InfiltrationW
- GroundW
- SolarGainsW
- InternalGainsW

These magnitude fields are non-negative and represent the absolute component activity.

The signed balance fields are:

- TransmissionBalanceW
- VentilationBalanceW
- InfiltrationBalanceW
- GroundBalanceW

The sign convention is:

- Positive value means heat gain to the room, zone or building.
- Negative value means heat loss from the room, zone or building.

Examples:

- Outdoor temperature lower than operative temperature produces negative TransmissionBalanceW.
- Outdoor temperature higher than operative temperature produces positive TransmissionBalanceW.
- Ground boundary temperature lower than operative temperature produces negative GroundBalanceW.
- Ground boundary temperature higher than operative temperature produces positive GroundBalanceW.

At annual aggregation level, the signed hourly values are exposed as:

- NetTransmissionKWh
- NetVentilationKWh
- NetInfiltrationKWh
- NetGroundKWh

These values preserve the sign of heat flow over the calculation period.

### Current limitation

The current true hourly path does not expose infiltration as a separate split.

If infiltration is modelled by the active hourly calculation path, it may be included in the combined ventilation contribution.

Because of that:

- InfiltrationW may remain 0.
- InfiltrationBalanceW may remain 0.
- NetInfiltrationKWh may remain 0.

This does not necessarily mean that the building has no physical infiltration. It means that the current hourly source does not expose infiltration separately.

The verification diagnostics should report this with:

- AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable

### Verification status

Signed component balance is internally deterministic tested for the currently available true hourly components.

Status:

- Transmission signed balance: InternalDeterministicTested
- Ventilation signed balance: InternalDeterministicTested
- Ground signed balance: InternalDeterministicTested
- Infiltration signed balance: Partial

This is not ExternalParityCovered.

External parity requires benchmark comparison fixtures with documented source results and tolerances.

END SECTION