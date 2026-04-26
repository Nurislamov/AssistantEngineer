# EnergyCalculationParity fixtures

Эта папка хранит входные и ожидаемые выходные данные для сверки AssistantEngineer с эталонной расчётной моделью.

## Правило

Каждый fixture должен содержать:

1. fixture name;
2. source reference;
3. reference version / commit / date;
4. input data;
5. expected hourly output;
6. expected monthly output;
7. expected annual output;
8. tolerance;
9. known assumptions.

## План fixtures

### P0

#### single-zone-no-solar.json

Назначение:

- одна зона;
- без солнечных поступлений;
- постоянная наружная температура;
- проверка transmission + ventilation + internal gains.

#### single-zone-solar-south-window.json

Назначение:

- одна зона;
- южное окно;
- проверка solar gains;
- проверка surface irradiance.

#### single-zone-annual-8760.json

Назначение:

- один полный год;
- 8760 часов;
- проверка annual heating/cooling need;
- проверка monthly aggregation.

### P1

#### multi-zone-adiabatic-wall.json

Назначение:

- две отапливаемые зоны;
- внутренняя стена между зонами;
- проверка adiabatic boundary.

#### adjacent-unheated-zone.json

Назначение:

- отапливаемая зона;
- соседняя неотапливаемая зона;
- проверка adjusted heat transfer.

#### dhw-residential.json

Назначение:

- бытовая горячая вода;
- объём;
- энергия;
- годовая агрегация.

#### primary-energy-heating-system.json

Назначение:

- delivered energy;
- final energy;
- primary energy;
- carrier factors.

## Tolerance policy

На первом этапе:

- hourly temperature: ±0.05 °C;
- hourly load: ±1 W;
- monthly demand: ±0.01 kWh;
- annual demand: ±0.1 kWh.

Если reference calculation использует округления или другие assumptions, tolerance может быть расширен, но только с комментарием.