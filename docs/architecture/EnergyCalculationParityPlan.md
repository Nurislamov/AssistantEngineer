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
| ISO52010.CLIMATE_CONVERSION | ISO 52010 external climate conversion | partial |
| ISO52010.SURFACE_IRRADIANCE | Solar irradiance on tilted/oriented surfaces | partial |
| WEATHER.EPW | EPW weather input normalization | partial |
| ISO52016.HOURLY_HEATING_NEED | Hourly sensible heating need | partial |
| ISO52016.HOURLY_COOLING_NEED | Hourly sensible cooling need | partial |
| ISO52016.MONTHLY_HEATING_COOLING_NEED | Monthly heating/cooling need | partial |
| ISO52016.INTERNAL_TEMPERATURE_HOURLY | Hourly internal / operative temperature | partial |
| ISO52016.SENSIBLE_LOAD_HOURLY | Hourly sensible heating/cooling load | partial |
| ISO52016.THERMAL_ZONES | Thermal zone calculation | partial |

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

## Fixtures still needed

| Needed fixture | Purpose |
|---|---|
| External reference single-zone annual ISO 52016 case | Real parity proof against an independently produced reference result |
| Multi-zone adjacent conditioned zone | Verify adiabatic/internal separating-wall behavior |
| Adjacent unconditioned zone | Verify adjusted adjacent-zone temperature and heat-transfer coefficient |
| EPW weather normalization fixture | Verify 8760 weather import and ISO 52010 conversion |
| PVGIS weather normalization fixture | Verify PVGIS import normalization |
| Surface irradiance N/E/S/W/horizontal fixture | Verify ISO 52010 oriented surface irradiance |
| DHW EN 12831-3 fixture | Verify domestic hot water demand |
| Primary energy EN 15316-1 fixture | Verify delivered/final/primary energy aggregation |
