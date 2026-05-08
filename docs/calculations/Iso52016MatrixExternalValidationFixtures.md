# ISO 52016 Matrix external validation fixtures

These fixtures provide the first independent manual validation anchors for the ISO 52016 Matrix solver.

## Scope

The fixtures use one air node, one outdoor boundary and one hour. The expected HVAC load is computed from the independent steady-state formula:

```text
heatingLoadW = max(0, H * (T_heat_setpoint - T_outdoor) - gains)
coolingLoadW = max(0, H * (T_outdoor - T_cool_setpoint) + gains)
```

where `H` is the heat transfer coefficient in W/K.

## Fixture set

| Fixture | Manual check |
| --- | --- |
| `manual-steady-state-heating.json` | Heating load without gains. |
| `manual-steady-state-heating-with-gains.json` | Heating load reduced by internal gains. |
| `manual-steady-state-cooling.json` | Cooling load without internal gains. |
| `manual-steady-state-cooling-with-gains.json` | Cooling load increased by internal gains. |

## Non-claim

These are manual engineering validation anchors. They do not claim full ISO 52016, StandardReference, EnergyPlus or ASHRAE 140 equivalence.