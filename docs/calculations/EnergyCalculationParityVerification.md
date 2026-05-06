# Energy Calculation Parity Verification

Verification is based on internal deterministic fixtures, focused engine tests, and application pipeline tests where the real backend path has been integrated.

The engineering calculation mode comparison and disclosure rollup is an internal governance layer for default versus opt-in transparency and deterministic delta reporting. It is not an external parity or certification signal.

## Current Status

| Function | Status | Evidence |
| --- | --- | --- |
| Transmission Heat Transfer | InternalDeterministicTested | Engine tests, transmission fixtures, room load application pipeline integration |
| Window Solar Gains | BenchmarkCompared | Engine tests, solar fixtures, room cooling application pipeline integration, annual-climate source/fallback diagnostics, deterministic window/surface benchmark fixtures; not ExternalParityCovered |
| Ventilation and Infiltration Loads | InternalDeterministicTested | Engine tests, ventilation fixtures, room load application pipeline integration, default ACH diagnostics |
| Internal Gains | InternalDeterministicTested | Engine tests, room cooling application pipeline integration, design-point schedule assumption diagnostics |
| Room Heating Load | InternalDeterministicTested | Room load engine tests, fixtures, application pipeline tests, endpoint facade integration, requested/actual method diagnostics |
| Room Cooling Load | InternalDeterministicTested | Room load engine tests, fixtures, application pipeline tests, endpoint facade integration, requested/actual method diagnostics |
| Thermal Zone Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, application pipeline tests |
| Floor Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, floor application pipeline tests |
| Building Aggregation | InternalDeterministicTested | Aggregation engine tests, fixtures, building application pipeline tests |
| Annual Energy Balance | BenchmarkCompared | InternalDeterministicTested by annual engine tests, fixtures, application pipeline adapter tests, hourly component mapper tests, report facade integration, monthly adapter/hourly source diagnostics; BenchmarkCompared for constant hourly deterministic benchmark fixtures, deterministic infiltration benchmark fixture, and deterministic ventilation split fixture; not ExternalParityCovered |
| Signed Component Balance | BenchmarkCompared | InternalDeterministicTested for signed hourly transmission, mechanical ventilation, natural ventilation, aggregate ventilation, infiltration and ground components; BenchmarkCompared for deterministic signed component benchmark fixtures including infiltration and ventilation split; not ExternalParityCovered |
| DHW Demand | InternalDeterministicTested | DHW deterministic service tests, fixtures, endpoint facade path; compatibility path remains default and ISO12831-3-inspired path is opt-in |
| System Energy | InternalDeterministicTested | System energy tests, fixtures, heating/cooling system services call SystemEnergyEngine; compatibility path remains default and EN15316-inspired modular chain is opt-in |
| Equipment Sizing Integration | InternalDeterministicTested | Equipment sizing tests, fixtures, room equipment application pipeline tests, endpoint/report integration, heating capacity diagnostics |

No function is marked ExternalParityCovered in this pass.

## Real Application Pipeline

The real backend calculation path now uses `EnergyCalculationPipelineService` behind `ILoadCalculationsFacade`.

- Room heating and cooling endpoints assemble room, envelope, ventilation, infiltration, ground, solar and internal-gain inputs, then call `RoomLoadCalculationEngine`. Solar design-point input prefers annual climate solar data through the centralized surface irradiance path and reports reference-by-orientation fallback when used. `requestedMethod`, `actualMethod` and compatibility warnings are exposed where the public API method differs from the actual Energy Calculation Parity design-point pipeline.
- Floor and building load endpoints consume room load results and call `LoadAggregationEngine` in design-point mode. Diagnostics identify the design-point aggregation mode when hourly source data is not available.
- Building energy balance uses the existing building energy source as an explicit adapter and feeds `AnnualEnergyBalanceEngine`. Diagnostics expose `TrueHourlySimulation` versus `MonthlyBalanceAdapter`, `hourlyRecordCount`, and `isTrueHourly8760`; representative monthly records are always false. True hourly source records now pass available transmission, mechanical ventilation, natural ventilation, aggregate ventilation, separate infiltration, ground, solar and internal-gain components through to the annual engine.
- DHW remains on the deterministic `DomesticHotWaterDemandService` facade path by default. An ISO12831-3-inspired DHW calculator is available as an opt-in integration path; this is still internal deterministic engineering evidence only and not a full compliance claim.
- Heating and cooling system services call `SystemEnergyEngine`, preserving useful, final and primary energy as separate values. Compatibility `SystemEnergyEngine` behavior remains default, and the EN15316-inspired modular chain is available as an explicit opt-in path.
- Room equipment selection uses the actual room load, separate project heating/cooling safety factors, `EquipmentSizingEngine`, and the active equipment catalog provider. Heating capacity is evaluated when catalog candidates expose it; otherwise diagnostics state that heating sizing is skipped. Empty catalogs and rejected candidates return diagnostics instead of silent selections.
- Cooling, heating and energy-balance reports consume facade results built from the same application pipeline. Excel generation stays in Infrastructure integrations.
- Benchmark fixture verification compares fixed expected benchmark/reference values with AssistantEngineer results through test-only comparison helpers. The active benchmark fixtures currently cover annual constant hourly deterministic cases, signed component balance deterministic cases, and deterministic window/surface solar cases. This is still only benchmark evidence when a comparison test actually runs and passes.

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
- Active benchmark fixtures must load and pass tolerance comparison.
- Pending and disabled benchmark fixtures must be skipped by default and reported by the loader.

## Closed Calculation Review Items

- Unified calculation diagnostics, runtime hourly-climate split, and `VentilationParameters` validation source-of-truth cleanup are closed.
- Equipment selector contracts are Result-based; equipment selection responses are populated from `EquipmentSizingResult` where the application pipeline is used, with explicit legacy adapter diagnostics for the old cooling selector.
- Cooling DTO naming now treats `CoolingLoadW`/`CoolingLoadKw` as canonical and keeps `TotalHeatLoadW`/`TotalHeatLoadKw` only as compatibility aliases.
- Peak-hour DTOs use nullable `PeakHourOfYear` where an annual hour is meaningful; design-point paths leave it null.
- Ground-contact enum mapping is strict for unmapped values, and `TimeProvider.System` is registered once through DI composition.

## Known Limits

The current status proves internal deterministic consistency, deterministic benchmark comparison for selected annual/signed component fixtures, and real application pipeline integration for the listed backend paths. The load-calculations annual endpoint is not a full 8760 simulation unless the upstream source supplies 8760 hourly records and the result says `energyDataSource = TrueHourlySimulation`. The true hourly component breakdown separates mechanical ventilation, natural ventilation, and infiltration when the source data can evaluate them. The design-point room load path is not full ISO hourly balance. No status in this matrix proves external benchmark parity.

## Solar/weather layer

The ISO52010-inspired solar/weather layer is internally deterministic tested for:

- solar position at representative equinox, winter and night conditions;
- isotropic sky surface irradiance;
- night solar clamping to zero;
- orientation/tilt influence on surface irradiance;
- window solar gain from total/component surface irradiance;
- room-level solar gain aggregation from hourly surface records;
- application pipeline annual-climate source and reference-by-orientation fallback diagnostics.

Current source diagnostics include:

- `SolarWeather.HourlyWeatherSourceUsed`;
- `SolarWeather.AnnualClimateSolarDataUsed`;
- `SolarWeather.ReferenceByOrientationFallbackUsed`;
- `SolarWeather.SyntheticWeatherUsed`;
- `SolarWeather.MissingDirectDiffuseSolarData`;
- `SolarWeather.NightSolarClampedToZero`;
- `SolarWeather.SurfaceIrradianceCalculated`.

Window solar gains are `BenchmarkCompared` for deterministic window fixtures. Surface irradiance night clamping is also `BenchmarkCompared` through deterministic benchmark fixtures. This does not claim `ExternalParityCovered`.
START SECTION

## Signed hourly component balance

The true hourly simulation path now supports signed component balance fields for available hourly components.

The existing magnitude fields remain available:

- TransmissionW
- VentilationW
- MechanicalVentilationW
- NaturalVentilationW
- InfiltrationW
- GroundW
- SolarGainsW
- InternalGainsW

These magnitude fields are non-negative and represent the absolute component activity.

The signed balance fields are:

- TransmissionBalanceW
- VentilationBalanceW
- MechanicalVentilationBalanceW
- NaturalVentilationBalanceW
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
- NetMechanicalVentilationKWh
- NetNaturalVentilationKWh
- NetInfiltrationKWh
- NetGroundKWh

These values preserve the sign of heat flow over the calculation period.

### Ventilation and infiltration split

The true hourly path separates infiltration from mechanical and natural ventilation.

Current field meanings:

- `MechanicalVentilationW`: mechanical ventilation magnitude.
- `NaturalVentilationW`: natural ventilation magnitude.
- `VentilationW`: `MechanicalVentilationW + NaturalVentilationW`.
- `InfiltrationW`: separate infiltration magnitude.
- `MechanicalVentilationBalanceW`: signed mechanical ventilation balance.
- `NaturalVentilationBalanceW`: signed natural ventilation balance.
- `VentilationBalanceW`: `MechanicalVentilationBalanceW + NaturalVentilationBalanceW`.
- `InfiltrationBalanceW`: signed infiltration balance.

`InfiltrationW` may still be 0 when infiltration assumptions are explicitly zero.

When a source cannot expose infiltration separately, diagnostics should report this with:

- AnnualEnergy.InfiltrationBalanceNotSeparatelyAvailable

### Verification status

Signed component balance is internally deterministic tested for the currently available true hourly components and benchmark compared against deterministic signed component fixtures.

Status:

- Transmission signed balance: BenchmarkCompared for deterministic signed component benchmark fixtures
- Ventilation signed balance: BenchmarkCompared for deterministic signed component benchmark fixtures
- Mechanical ventilation signed balance: BenchmarkCompared for deterministic ventilation split benchmark fixture
- Natural ventilation signed balance: BenchmarkCompared for deterministic ventilation split benchmark fixture
- Infiltration signed balance: BenchmarkCompared for deterministic infiltration benchmark fixture
- Ground signed balance: BenchmarkCompared for deterministic signed component benchmark fixtures

This is not ExternalParityCovered.

External parity requires benchmark comparison fixtures with documented source results and tolerances.

END SECTION
