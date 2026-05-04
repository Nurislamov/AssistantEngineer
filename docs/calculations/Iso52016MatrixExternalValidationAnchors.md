# ISO 52016 Matrix external validation anchors

This document tracks the first external-validation-anchor layer for the ISO 52016 Matrix solver.

## Scope

These cases are **validation anchors only**. They are intentionally small, independent, and manually auditable.

They do not claim:

- exact pyBuildingEnergy numerical parity;
- exact EnergyPlus numerical parity;
- ASHRAE 140 validation coverage;
- full external engine parity.

## Fixture families

| Fixture | Reference style | Claim scope | Purpose |
| --- | --- | --- | --- |
| `manual-independent-steady-heating.json` | Manual | ValidationAnchorOnly | One-hour heating anchor without gains. |
| `manual-independent-steady-heating-with-gains.json` | Manual | ValidationAnchorOnly | One-hour heating anchor with sensible gains offsetting load. |
| `manual-independent-steady-cooling.json` | Manual | ValidationAnchorOnly | One-hour cooling anchor without gains. |
| `pbe-style-manual-steady-heating.json` | pyBuildingEnergy-style naming only | ValidationAnchorOnly | Naming convention anchor; numeric reference remains independent manual formula. |
| `energyplus-style-annual-manual-8760.json` | EnergyPlus-style naming only | ValidationAnchorOnly | Compact annual 8760 anchor generated from twelve manual outdoor-temperature blocks. |

## Manual formulas

Single-hour steady heating anchor:

```text
heatingLoadW = max(0, H * (T_heat_setpoint - T_outdoor) - gains)
```

Single-hour steady cooling anchor:

```text
coolingLoadW = max(0, H * (T_outdoor - T_cool_setpoint) + gains)
```

Annual 8760 anchor:

```text
T_free = ((C / dt) * T_previous + H * T_outdoor + gains) / ((C / dt) + H)
```

The annual anchor then applies the same heating/cooling setpoint control as the Matrix solver and compares annual energy, peak loads, and hour count.

## Verification

Run:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1
```

This script checks the fixture set, manifest, documentation, guard tests, and the Matrix solver results for the first anchor batch.

## Explicit non-claims

- No exact pyBuildingEnergy numerical parity claim.
- No exact EnergyPlus numerical parity claim.
- No ASHRAE 140 validation coverage claim.
- No full annual dynamic simulation parity claim.
- Validation anchors only, not full parity.

## Step 02 expanded independent anchor set

The anchor set now includes at least 10 source-controlled JSON fixtures:

- independent steady heating/cooling anchors;
- a neutral deadband anchor;
- a zero-load heating/gains balance anchor;
- a cooling-from-gains-only anchor;
- pyBuildingEnergy-style naming anchors;
- EnergyPlus-style naming anchors;
- one compact annual 8760 manual reference anchor.

These are still validation anchors only. The added names are compatibility-style naming anchors and do not create full pyBuildingEnergy parity, full EnergyPlus parity, or ASHRAE 140 validation claims.
