# ISO 52016 Matrix external validation anchors

This stage adds independent manual engineering validation anchors for the ISO 52016 Matrix solver.

## Status

Status: **validation anchors only, not full parity**.

The fixtures in this stage are intentionally small and independently checkable. They are not pyBuildingEnergy outputs, EnergyPlus outputs, ASHRAE 140 cases, or full ISO 52016 conformance claims.

## Source policy

Authoritative reference for these fixtures:

```text
IndependentManualEngineeringFormula
```

Allowed background naming/style references:

```text
pyBuildingEnergy-style naming only; no output parity claim
EnergyPlus-style naming only; no output parity claim
```

Disallowed claims:

```text
No pyBuildingEnergy parity claim.
No EnergyPlus parity claim.
No ASHRAE 140 validation coverage claim.
No full ISO 52016 parity claim.
```

## Fixtures

| Id | File | Independent manual anchor |
| --- | --- | --- |
| `MANUAL-ISO52016-ANCHOR-001` | `manual-iso52016-anchor-001-steady-heating.json` | Steady heating: `Q_heat = H Г— О”T`. |
| `MANUAL-ISO52016-ANCHOR-002` | `manual-iso52016-anchor-002-heating-with-gains.json` | Heating with gains: `Q_heat = H Г— О”T - gains`. |
| `MANUAL-ISO52016-ANCHOR-003` | `manual-iso52016-anchor-003-steady-cooling.json` | Steady cooling: `Q_cool = H Г— О”T + gains`. |
| `MANUAL-ISO52016-ANCHOR-004` | `manual-iso52016-anchor-004-free-floating-no-hvac.json` | Free-floating no-HVAC implicit temperature response. |
| `MANUAL-ISO52016-ANNUAL-8760-001` | `manual-iso52016-annual-8760-001-constant-weather-heating.json` | Annual 8760 constant-weather heating energy: hourly manual load multiplied by 8760. |

## Manual formulas

### Heating

```text
Q_heat = max(0, H * (T_heat_setpoint - T_outdoor) - gains)
```

The heating fixtures set previous air temperature equal to the heating setpoint. That removes storage from the controlled load equation and leaves a directly auditable steady-state anchor.

### Cooling

```text
Q_cool = max(0, H * (T_outdoor - T_cool_setpoint) + gains)
```

The cooling fixture sets previous air temperature equal to the cooling setpoint. That removes storage from the controlled load equation and leaves a directly auditable steady-state anchor.

### Free-floating no HVAC

```text
T_next = ((C / dt) * T_initial + H * T_outdoor + gains) / ((C / dt) + H)
```

The free-floating fixture keeps `T_next` inside the heating/cooling deadband. Expected heating and cooling loads are both zero.

### Annual 8760 constant-weather reference

```text
E_heat_annual = Q_heat_hourly * 8760 h / 1000
```

The annual fixture uses constant outdoor temperature, gains, conductance and setpoints for all 8760 hours. This is an annual arithmetic anchor, not an external tool parity case.

## Verification

```powershell
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1
```

The all-in-one Matrix verification also includes this stage:

```powershell
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
```