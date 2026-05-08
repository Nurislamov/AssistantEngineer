# ExternalReferenceValidation fixtures

��� ����� ������ ������� � ��������� �������� ������ ��� ������ AssistantEngineer � ��������� ��������� �������.

## �������

������ fixture ������ ���������:

1. fixture name;
2. source reference;
3. reference version / commit / date;
4. input data;
5. expected hourly output;
6. expected monthly output;
7. expected annual output;
8. tolerance;
9. known assumptions.

## ���� fixtures

### P0

#### single-zone-no-solar.json

����������:

- ���� ����;
- ��� ��������� �����������;
- ���������� �������� �����������;
- �������� transmission + ventilation + internal gains.

#### single-zone-solar-south-window.json

����������:

- ���� ����;
- ����� ����;
- �������� solar gains;
- �������� surface irradiance.

#### single-zone-annual-8760.json

����������:

- ���� ������ ���;
- 8760 �����;
- �������� annual heating/cooling need;
- �������� monthly aggregation.

### P1

#### multi-zone-adiabatic-wall.json

����������:

- ��� ������������ ����;
- ���������� ����� ����� ������;
- �������� adiabatic boundary.

#### adjacent-unheated-zone.json

����������:

- ������������ ����;
- �������� �������������� ����;
- �������� adjusted heat transfer.

#### dhw-residential.json

����������:

- ������� ������� ����;
- �����;
- �������;
- ������� ���������.

#### primary-energy-heating-system.json

����������:

- delivered energy;
- final energy;
- primary energy;
- carrier factors.

## Tolerance policy

�� ������ �����:

- hourly temperature: �0.05 �C;
- hourly load: �1 W;
- monthly demand: �0.01 kWh;
- annual demand: �0.1 kWh.

���� reference calculation ���������� ���������� ��� ������ assumptions, tolerance ����� ���� ��������, �� ������ � ������������.