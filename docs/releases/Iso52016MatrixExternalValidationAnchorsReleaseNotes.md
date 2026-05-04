# ISO 52016 Matrix external validation anchors release notes

## Status

Closed as `ValidationAnchorOnly`.

The stage provides independent manual engineering validation anchors for the ISO 52016 Matrix solver path. It does not provide, imply, or approximate full pyBuildingEnergy parity, EnergyPlus parity, ASHRAE 140 validation, or full ISO 52016 conformance certification.

## Closed anchors

| Anchor | Purpose | Manual reference |
| --- | --- | --- |
| `MANUAL-ISO52016-ANCHOR-001` | Steady heating | `Q = H * (T_heat_setpoint - T_outdoor)` |
| `MANUAL-ISO52016-ANCHOR-002` | Heating with gains | `Q = H * (T_heat_setpoint - T_outdoor) - gains` |
| `MANUAL-ISO52016-ANCHOR-003` | Steady cooling | `Q = H * (T_outdoor - T_cool_setpoint) + gains` |
| `MANUAL-ISO52016-ANCHOR-004` | Free-floating response with no HVAC | first-order single-node response |
| `MANUAL-ISO52016-ANNUAL-8760-001` | Annual constant-weather reference | constant hourly heating load integrated over 8760 hours |

## Evidence commands

```powershell
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1
.\scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
.\scripts\iso52016\verify-iso52016-matrix-all.ps1
```

## Generated artifacts

Optional merge summaries are generated under:

```text
artifacts/iso52016/external-validation-anchors/
```

They are generated evidence outputs and must not be committed.

## Explicit non-claims

No pyBuildingEnergy parity is claimed or implied.

This release is validation anchors only, not full parity.

## Non-claims

Validation anchors only, not full parity.

No exact pyBuildingEnergy numerical parity claim.
No exact EnergyPlus numerical parity claim.
No ExternalParityCovered claim.
No FullParityCovered claim.
No pyBuildingEnergy parity.

No full ISO 52016 parity claim.

