# ISO 52016 Matrix external validation anchors

This stage adds independent manual engineering validation anchors for the ISO 52016 Matrix-only hourly solver.

Status: validation anchors only, not full parity.

## Scope for patch 2.1.1

This patch creates the Stage 2.1 anchor structure and the first three single-hour manual fixtures:

| Anchor | Manual expectation | Status |
| --- | --- | --- |
| `MANUAL-ISO52016-ANCHOR-001` | `Q_heat = H x (T_heat,set - T_out)` | Added |
| `MANUAL-ISO52016-ANCHOR-002` | `Q_heat = H x (T_heat,set - T_out) - gains` | Added |
| `MANUAL-ISO52016-ANCHOR-003` | `Q_cool = H x (T_out - T_cool,set) + gains` | Added |

Each fixture uses one air node, one outdoor boundary and one hourly record. The air node initial temperature is set to the target setpoint so that the transient heat-capacity term cancels and the expected HVAC load is the independent steady engineering formula above.

## Explicit non-claims

These fixtures are validation anchors only.

They do not claim:

- full ISO 52016 validation coverage;
- exact pyBuildingEnergy numerical parity;
- exact EnergyPlus numerical parity;
- ASHRAE 140 coverage;
- annual/weather-file parity.

`pyBuildingEnergy` remains methodological background only. Its outputs are not used as authoritative references for these anchors.

## Next anchors not included in this patch

The following are intentionally left for follow-up patch scripts:

- `MANUAL-ISO52016-ANCHOR-004` free-floating no-HVAC temperature response;
- `MANUAL-ISO52016-ANNUAL-8760-001` annual constant-weather reference;
- final Stage 2.1 merge/release summary.

## Verification

```powershell
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1
```

This command checks the fixture/docs/manifest structure and runs the C# guard tests for the manual anchors.