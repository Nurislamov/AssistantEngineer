# EnergyPlus Validation Case Template

Use this template when adding a new validation case.

## Case metadata

- Case id:
- Case name:
- Stage:
- Source:
- Weather source:
- Geometry:
- Envelope:
- Internal gains:
- Ventilation:
- HVAC control:

## AssistantEngineer setup

- Building geometry:
- Envelope U-values:
- Window assumptions:
- Solar assumptions:
- Internal gains assumptions:
- Ventilation/infiltration assumptions:
- Heating setpoint:
- Cooling setpoint:
- Weather source:
- Calculation method:
- Actual method:

## Reference setup

- Reference engine:
- Reference version:
- Reference weather:
- Reference model file:
- Reference output file:
- Exported metrics:
- Run date:

## Metrics

| Metric id | Name | Unit | AssistantEngineer value | Reference value | Tolerance | Type | Notes |
|---|---|---:|---:|---:|---:|---|---|
| annual-heating-kwh | Annual heating energy | kWh | | | | NumericWithinTolerance | |
| annual-cooling-kwh | Annual cooling energy | kWh | | | | NumericWithinTolerance | |
| peak-heating-w | Peak heating load | W | | | | NumericWithinTolerance | |
| peak-cooling-w | Peak cooling load | W | | | | NumericWithinTolerance | |

## Assumptions

- 
- 
- 

## Known differences

- 
- 
- 

## Non-claims

- Does not claim exact EnergyPlus numerical equivalence.
- Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.
- Does not claim full ISO 52016 node/matrix solver equivalence.
