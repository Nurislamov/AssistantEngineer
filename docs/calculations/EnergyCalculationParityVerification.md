# Energy Calculation Parity Verification

Verification is based on internal deterministic fixtures, focused engine tests, and application pipeline tests where the real backend path has been integrated.

## Current Status

| Function | Status | Evidence |
| --- | --- | --- |
| Transmission Heat Transfer | InternalDeterministicTested | Engine tests, transmission fixtures, room load application pipeline integration |
| Window Solar Gains | InternalDeterministicTested | Engine tests, solar fixtures, room cooling application pipeline integration |
| Ventilation and Infiltration Loads | InternalDeterministicTested | Engine tests, ventilation fixtures, room load application pipeline integration |
| Internal Gains | InternalDeterministicTested | Engine tests, room cooling application pipeline integration |
| Room Heating Load | InternalDeterministicTested | Room load engine tests, fixtures, application pipeline tests, endpoint facade integration |
| Room Cooling Load | InternalDeterministicTested | Room load engine tests, fixtures, application pipeline tests, endpoint facade integration |
| Thermal Zone Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, application pipeline tests |
| Floor Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, floor application pipeline tests |
| Building Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, building application pipeline tests |
| Annual Energy Balance | InternalDeterministicTested | Annual engine tests, fixtures, application pipeline adapter tests, report facade integration |
| DHW Demand | InternalDeterministicTested | DHW deterministic service tests, fixtures, endpoint facade path |
| System Energy | InternalDeterministicTested | System energy tests, fixtures, heating/cooling system services call SystemEnergyEngine |
| Equipment Sizing Integration | InternalDeterministicTested | Equipment sizing tests, fixtures, room equipment application pipeline tests, endpoint/report integration |

No function is marked ExternalParityCovered in this pass.

## Real Application Pipeline

The real backend calculation path now uses `EnergyCalculationPipelineService` behind `ILoadCalculationsFacade`.

- Room heating and cooling endpoints assemble room, envelope, ventilation, infiltration, ground, solar and internal-gain inputs, then call `RoomLoadCalculationEngine`.
- Floor and building load endpoints consume room load results and call `LoadAggregationEngine` in design-point mode. Diagnostics identify the design-point aggregation mode when hourly source data is not available.
- Building energy balance uses the existing building energy source as an explicit adapter and feeds `AnnualEnergyBalanceEngine`. Diagnostics expose the hourly/monthly source as synthetic profile or unavailable.
- DHW remains on the deterministic `DomesticHotWaterDemandService` facade path.
- Heating and cooling system services call `SystemEnergyEngine`, preserving useful, final and primary energy as separate values.
- Room equipment selection uses the actual room cooling load, project safety factor, `EquipmentSizingEngine`, and the active equipment catalog provider. Empty catalogs and rejected candidates return diagnostics instead of silent selections.
- Cooling, heating and energy-balance reports consume facade results built from the same application pipeline. Excel generation stays in Infrastructure integrations.

## Regression Rules

- Non-pending fixtures must load.
- Deterministic fixtures must pass within tolerance.
- Annual totals must equal monthly sums within tolerance.
- Design building loads must equal sums of unique room loads in design-point mode.
- Diagnostics must exist for fallback or clamped assumptions.
- Room cooling separates solar, internal, transmission and ventilation components.
- Room heating separates transmission, ventilation, infiltration and ground components.
- Equipment sizing explains rejected candidates.

## Known Limits

The current status proves internal deterministic consistency and real application pipeline integration for the listed backend paths. It does not prove external benchmark parity.
