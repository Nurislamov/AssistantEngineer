# ISO 52016 Matrix engineering edge cases

This stage hardens the ISO 52016 Matrix solver with internal engineering edge-case anchors.

## Scope

These fixtures are **engineering hardening anchors**, not external equivalence fixtures.

They cover:

1. `ENGINEERING-ISO52016-MATRIX-EDGE-001` - two-node free-floating implicit thermal response.
2. `ENGINEERING-ISO52016-MATRIX-EDGE-002` - adjacent unconditioned boundary heating load.
3. `ENGINEERING-ISO52016-MATRIX-EDGE-003` - timestep energy scaling for a steady controlled load.
4. `ENGINEERING-ISO52016-MATRIX-EDGE-004` - internal gain sign conventions.
5. `ENGINEERING-ISO52016-MATRIX-EDGE-005` - monthly and annual aggregation edge cases.

## Engineering formulas guarded

For steady controlled one-node cases where the air node starts at the active setpoint:

```text
heatingLoadW = max(0, H * (T_heat_setpoint - T_boundary) - gains)
coolingLoadW = max(0, H * (T_boundary - T_cool_setpoint) + gains)
energyKWh = loadW * timeStepSeconds / 3600 / 1000
```

For the two-node free-floating case, the expected response is calculated independently from the 2x2 implicit Euler system for one air node and one massive node.

## Adjacent/unconditioned boundary policy

The adjacent unconditioned boundary anchor is intentionally represented as a named boundary temperature input. It verifies the solver's conductance and sign behavior for adjacent-zone style boundaries without claiming a complete adjacent-zone model.

## Non-claims

Engineering edge-case hardening only.

Validation anchors only, not full equivalence claim.

No StandardReference equivalence claim.
No EnergyPlus comparison workflow claim.
No ASHRAE 140 / BESTEST-style validation anchor coverage claim.
No full ISO 52016 equivalence claim.
No adjacent-zone full model equivalence claim.
No full annual building simulation equivalence claim.