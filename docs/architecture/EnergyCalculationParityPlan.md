# Energy calculation parity plan

Цель AssistantEngineer — реализовать расчётное ядро, которое функционально покрывает выбранную эталонную модель расчёта энергоэффективности зданий.

## Важное правило нейминга

В production-коде, namespace, class names, folder names и test namespace AssistantEngineer не используется имя внешнего проекта или библиотеки.

Правильно:

- EnergyCalculationParity
- ReferenceFeatureStatus
- EnergyCalculationParityMatrix
- EnergyCalculationParityFeature
- EnergyCalculationParity fixtures

Неправильно:

- имя внешней библиотеки в namespace;
- имя внешней библиотеки в class name;
- имя внешней библиотеки в folder name;
- имя внешней библиотеки в production architecture.

Внешний проект может использоваться как reference implementation только вне архитектурного нейминга AssistantEngineer.

## Что такое parity

Parity означает, что AssistantEngineer на одинаковых входных данных должен выдавать результаты, совпадающие с выбранным эталонным расчётом в пределах заданной tolerance.

Фича считается covered только если есть:

1. production implementation;
2. unit tests на формулы;
3. reference fixture;
4. parity/reference test;
5. edge-case tests;
6. documented assumptions.

## Приоритеты

| Priority | Meaning |
|---|---|
| P0 | Критично для расчётного ядра |
| P1 | Нужно для полного покрытия расчётной модели |
| P2 | Интеграция, API, отчёты |
| P3 | Не входит в текущий scope |

## P0 — основа расчётного ядра

| Code | Feature | Current AssistantEngineer status |
|---|---|---|
| ENERGY_CALCULATION_PARITY.TRANSMISSION_HEAT_TRANSFER | Transmission heat transfer | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.WINDOW_SOLAR_GAINS | Window solar gains | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.VENTILATION_INFILTRATION_LOADS | Ventilation and infiltration loads | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.INTERNAL_GAINS | Internal gains | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.ROOM_HEATING_LOAD | Room heating load | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.ROOM_COOLING_LOAD | Room cooling load | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.THERMAL_ZONE_AGGREGATION | Thermal zone aggregation | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.FLOOR_AGGREGATION | Floor aggregation | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.BUILDING_AGGREGATION | Building aggregation | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.ANNUAL_ENERGY_BALANCE | Annual energy balance | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.DHW_DEMAND | DHW demand | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.SYSTEM_ENERGY | System energy | InternalDeterministicTested |
| ENERGY_CALCULATION_PARITY.EQUIPMENT_SIZING_INTEGRATION | Equipment sizing integration | InternalDeterministicTested |
| ISO52010.CLIMATE_CONVERSION | ISO 52010 external climate conversion | partial |
| ISO52010.SURFACE_IRRADIANCE | Solar irradiance on tilted/oriented surfaces | partial |
| WEATHER.EPW | EPW weather input normalization | partial |
| ISO52016.HOURLY_HEATING_NEED | Hourly sensible heating need | partial |
| ISO52016.HOURLY_COOLING_NEED | Hourly sensible cooling need | partial |
| ISO52016.MONTHLY_HEATING_COOLING_NEED | Monthly heating/cooling need | partial |
| ISO52016.INTERNAL_TEMPERATURE_HOURLY | Hourly internal / operative temperature | partial |
| ISO52016.SENSIBLE_LOAD_HOURLY | Hourly sensible heating/cooling load | partial |
| ISO52016.THERMAL_ZONES | Thermal zone calculation | partial |

## Real application pipeline

Реальные API и report paths должны идти через application-level orchestrator, а не напрямую через старые calculators. Текущий интегрированный путь:

- `GET /api/v1/rooms/{roomId}/load-calculations/heating-load` и `GET /api/v1/rooms/{roomId}/load-calculations/cooling-load` вызывают `ILoadCalculationsFacade`, затем `EnergyCalculationPipelineService`, который собирает input-модель комнаты и вызывает `RoomLoadCalculationEngine`.
- `GET /api/v1/floors/{floorId}/load-calculations/heating-load`, `GET /api/v1/floors/{floorId}/load-calculations/cooling-load`, building heating и building cooling routes используют room load results и `LoadAggregationEngine` в design-point mode.
- Public method query values are preserved as `requestedMethod`; results also expose `actualMethod` and diagnostics when the endpoint is currently using the Energy Calculation Parity design-point pipeline for API compatibility.
- Room ground boundaries are passed into transmission inputs. If ground-contact metadata or a ground temperature profile is missing, diagnostics state that explicitly; the pipeline does not silently treat ground as outdoor.
- Room solar gains prefer available annual/weather solar context. If it is unavailable, the orientation reference irradiance fallback is used with a diagnostic warning.
- Room ventilation falls back to project/default ACH only with diagnostics that include the value. Room responses expose the effective ACH/airflow values and source. Invalid default ACH returns validation/diagnostics.
- Design-point internal gains use full schedule factor `1.0` and report that assumption. Existing hourly analysis paths remain responsible for schedule expansion.
- `GET /api/v1/buildings/{buildingId}/load-calculations/energy-balance` uses an explicit annual aggregation adapter and then `AnnualEnergyBalanceEngine`. It tries existing true hourly simulation records first, maps them through `HourlySimulationToAnnualEnergyInputMapper`, and distinguishes `TrueHourlySimulation` from `MonthlyBalanceAdapter`. Results expose `isTrueHourly8760` and `hourlyRecordCount`; representative monthly records are not labelled as true 8760 simulation.
- `POST /api/v1/domestic-hot-water/demand` остается на deterministic DHW service path.
- Building energy analysis heating/cooling system routes используют `SystemEnergyEngine`; useful, final и primary energy не смешиваются.
- Building energy analysis routes remain a separate `ISO52016InspiredHourlyAnalysis`/monthly analysis mode. This path is labelled separately and is not silently mixed with the load-calculations annual adapter.
- `POST /api/v1/rooms/{roomId}/equipment-selection` берет actual room load из pipeline, применяет separate project heating/cooling safety factors, вызывает `EquipmentSizingEngine` и затем маппит accepted/rejected catalog candidates. Heating capacity is evaluated when catalog rows expose it; cooling capacity is evaluated against catalog cooling capacity; otherwise diagnostics state the limitation.
- Cooling, heating и energy-balance reports потребляют facade results, построенные из нового pipeline. ClosedXML остается только в Infrastructure integrations.
- Benchmark verification now asks `ILoadCalculationsFacade` for the AssistantEngineer building cooling result, so comparison reports use the application pipeline result. Benchmark statuses still do not imply external parity coverage without passing comparison evidence.

Старые calculators сохраняются как compatibility adapters или alternative method labels там, где они еще нужны для public contracts. Новые статусы выше `InternalDeterministicTested` не выставляются без benchmark comparison evidence. Для integrated paths notes должны содержать `Application pipeline integrated.`

### Current limitations

- The load-calculations energy-balance endpoint is an annual aggregation adapter unless the upstream source provides true 8760 hourly records. If neither true hourly records nor monthly balances are available, the application path returns validation instead of fake annual values.
- The design-point room load path is not a full hourly balance and does not claim full ISO compliance.
- Annual climate solar data is used when available; otherwise the orientation reference irradiance fallback remains documented and diagnosed.
- Internal schedules are expanded in existing hourly analysis paths, not in design-point room loads.
- Equipment heating selection depends on catalog heating capacity fields being populated.
- No feature is marked `ExternalParityCovered` in this pass.

## P1 — расширение до полного расчётного покрытия

| Code | Feature | Current AssistantEngineer status |
|---|---|---|
| WEATHER.PVGIS | PVGIS weather input normalization | partial |
| ISO52016.MULTI_ZONE | Multi-zone calculation | partial |
| ISO52016.ADJACENT_HEATED_ZONE | Adjacent heated zones / adiabatic walls | partial |
| ISO52016.ADJACENT_NON_HEATED_ZONE | Adjacent non-heated zones | partial |
| DHW.EN12831_3 | Domestic hot water volume and energy need | partial |
| PRIMARY_ENERGY.EN15316_1 | Primary energy calculation | partial |

## P3 — не входит в текущий scope

| Code | Feature | Reason |
|---|---|---|
| LATENT.ENERGY_NEED | Latent energy need | Не входит в текущий parity target |
| LATENT.MOISTURE_LOAD | Moisture / latent load | Не входит в текущий parity target |
| SUPPLY_AIR.HUMIDIFICATION_CONDITIONS | Supply-air humidification/dehumidification conditions | Не входит в текущий parity target |

## Порядок реализации

### Sprint 1

Создать EnergyCalculationParity matrix и guard-тесты.

### Sprint 2

ISO 52010 weather / solar layer.

### Sprint 3

ISO 52016 hourly calculation core.

### Sprint 4

Monthly aggregation and annual result.

### Sprint 5

Thermal zones, multi-zone, adjacent zones.

### Sprint 6

DHW calculation.

### Sprint 7

Primary energy.

### Sprint 8

API/reporting integration.

## Fixture policy

Каждый reference fixture должен содержать:

1. имя fixture;
2. описание здания / зоны;
3. входные данные;
4. ожидаемые hourly results;
5. ожидаемые monthly results;
6. ожидаемые annual results;
7. tolerance;
8. assumptions.

## Tolerance policy

Базовые значения:

- hourly temperature: ±0.05 °C;
- hourly load: ±1 W;
- monthly demand: ±0.01 kWh;
- annual demand: ±0.1 kWh.

Tolerance можно расширять только с documented assumption.

## Deterministic fixtures added

Эти fixtures являются deterministic reference fixtures AssistantEngineer. Они не являются external reference parity proof и не переводят matrix features в covered.

| Fixture | Scope | Test coverage |
|---|---|---|
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/single-zone-no-solar.json` | Single-zone hourly heat balance without solar gains | Heating/cooling load, fixed transmission and ventilation coefficients, internal gains |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/single-zone-solar-south-window.json` | Single south-facing window solar gains | Beam, diffuse sky, ground-reflected and total solar gains |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/single-zone-annual-8760.json` | Compact one-zone annual aggregation | 8760-hour month bins, monthly sums, annual totals, peak loads |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/transmission-single-external-wall-winter.json` | Transmission heat transfer | Single outdoor wall winter heat loss |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/transmission-single-window-winter.json` | Transmission heat transfer | Single outdoor window winter heat loss |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/transmission-adiabatic-internal-wall.json` | Transmission heat transfer | Internal adiabatic boundary exclusion and diagnostic |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/transmission-adjacent-conditioned-same-temperature.json` | Transmission heat transfer | Adjacent conditioned zone with zero temperature difference |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/transmission-outdoor-cooling-gain.json` | Transmission heat transfer | Outdoor cooling condition heat gain sign convention |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/window-solar-single-window-no-shading.json` | Window solar gains | Single window, no shading, provided incident irradiance |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/window-solar-single-window-with-shading.json` | Window solar gains | Frame, internal shading, external shading and fixed factors |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/window-solar-night-is-zero.json` | Window solar gains | Zero incident irradiance returns zero solar gain |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/window-solar-invalid-shgc-diagnostics.json` | Window solar gains | Invalid SHGC produces diagnostics and no gain |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/window-solar-room-aggregation.json` | Window solar gains | Room-level aggregation across two windows |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/ventilation-mechanical-heating-load.json` | Ventilation and infiltration loads | Mechanical ventilation winter heating load |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/ventilation-mechanical-cooling-load.json` | Ventilation and infiltration loads | Mechanical ventilation summer cooling load |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/ventilation-with-heat-recovery.json` | Ventilation and infiltration loads | Heat recovery reduces mechanical ventilation load |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/ventilation-infiltration-by-ach.json` | Ventilation and infiltration loads | Infiltration airflow from ACH and room volume |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/ventilation-zero-airflow.json` | Ventilation and infiltration loads | Zero airflow returns zero outdoor air load |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/ventilation-invalid-heat-recovery-efficiency.json` | Ventilation and infiltration loads | Invalid heat recovery efficiency produces diagnostics |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-occupancy-sensible.json` | Internal gains | Occupancy sensible gain |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-lighting-by-area.json` | Internal gains | Lighting gain by area |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-equipment-by-area.json` | Internal gains | Equipment gain by area |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-process-with-schedule.json` | Internal gains | Process gain with schedule |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-room-aggregation.json` | Internal gains | Room internal gain aggregation |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-zero-schedule.json` | Internal gains | Zero schedule gives zero gain |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-invalid-schedule-factor.json` | Internal gains | Invalid schedule diagnostic |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/internal-gains-negative-power-density.json` | Internal gains | Negative power density diagnostic |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/room-load-heating-transmission-only.json` | Room load | Heating transmission-only total and W/m2 |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/room-load-heating-transmission-ventilation-infiltration.json` | Room load | Heating transmission, ventilation and infiltration aggregation |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/room-load-cooling-solar-internal-ventilation.json` | Room load | Cooling component separation and W/m2 |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/room-load-does-not-go-negative.json` | Room load | Negative component clamp diagnostics |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/aggregation-floor-two-rooms.json` | Load aggregation | Floor sum, area and W/m2 |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/aggregation-building-two-floors.json` | Load aggregation | Building sum from room loads on two floors |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/aggregation-thermal-zone-no-double-count.json` | Load aggregation | Thermal zone de-duplication |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/annual-constant-heating-load.json` | Annual energy balance | 8760 hourly heating W to kWh |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/annual-constant-cooling-load.json` | Annual energy balance | 8760 hourly cooling W to kWh |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/annual-monthly-aggregation-consistency.json` | Annual energy balance | Annual total equals monthly sum |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/annual-energy-use-intensity.json` | Annual energy balance | EUI from annual total and area |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/dhw-residential-simple.json` | DHW demand | People-based DHW formula |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/dhw-zero-occupancy.json` | DHW demand | Zero occupancy diagnostic |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/system-heating-efficiency.json` | System energy | Useful heating divided by efficiency |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/system-cooling-cop.json` | System energy | Useful cooling divided by COP |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/system-total-energy.json` | System energy | Total final energy |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/equipment-sizing-cooling-simple.json` | Equipment sizing | Cooling reserve from cooling safety factor |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/equipment-candidate-accepted.json` | Equipment sizing | Candidate accepted and margin |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/equipment-candidate-rejected.json` | Equipment sizing | Candidate rejected with reason |
| `tests/AssistantEngineer.Tests/Parity/EnergyCalculationParity/Fixtures/equipment-no-equipment-found.json` | Equipment sizing | Empty catalog diagnostics |

## Fixtures still needed

| Needed fixture | Purpose |
|---|---|
| External reference single-zone annual ISO 52016 case | Real parity proof against an independently produced reference result |
| Multi-zone adjacent conditioned zone | Verify adiabatic/internal separating-wall behavior |
| Adjacent unconditioned zone | Verify adjusted adjacent-zone temperature and heat-transfer coefficient |
| EPW weather normalization fixture | Verify 8760 weather import and ISO 52010 conversion |
| PVGIS weather normalization fixture | Verify PVGIS import normalization |
| Surface irradiance N/E/S/W/horizontal fixture | Verify ISO 52010 oriented surface irradiance |
| External DHW benchmark fixture | Verify domestic hot water demand against a documented benchmark |
| External system energy benchmark fixture | Verify final/primary energy aggregation against a documented benchmark |
