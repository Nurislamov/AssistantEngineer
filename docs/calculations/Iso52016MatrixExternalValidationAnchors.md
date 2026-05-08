# ISO 52016 Matrix external validation anchors

This fixture set provides independent manual engineering validation anchors for the ISO 52016 Matrix solver path.

## Validation status

`ValidationAnchorOnly`.

These fixtures are not a StandardReference equivalence suite, not an EnergyPlus comparison workflow suite, not an ASHRAE 140 / BESTEST-style validation anchor suite, and not a full ISO 52016 conformance claim.

## Authoritative reference

The authoritative references for this stage are manual engineering formulas encoded directly in the fixture JSON files and guard tests.

StandardReference remains methodological background only. EnergyPlus-style names may be used for readability only. Neither StandardReference nor EnergyPlus outputs are authoritative references for this stage.

## Fixture set

| Fixture | Anchor | Manual reference |
| --- | --- | --- |
| `manual-iso52016-anchor-001-steady-heating.json` | `MANUAL-ISO52016-ANCHOR-001` | `Q = H * (T_heat_setpoint - T_outdoor)` |
| `manual-iso52016-anchor-002-heating-with-gains.json` | `MANUAL-ISO52016-ANCHOR-002` | `Q = H * (T_heat_setpoint - T_outdoor) - gains` |
| `manual-iso52016-anchor-003-steady-cooling.json` | `MANUAL-ISO52016-ANCHOR-003` | `Q = H * (T_outdoor - T_cool_setpoint) + gains` |
| `manual-iso52016-anchor-004-free-floating-no-hvac.json` | `MANUAL-ISO52016-ANCHOR-004` | no HVAC, first-order single-node response |
| `manual-iso52016-annual-8760-001-constant-weather-heating.json` | `MANUAL-ISO52016-ANNUAL-8760-001` | constant hourly heating reference integrated over 8760 hours |

## Verification

```powershell
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors.ps1
.\scripts\iso52016\verify-iso52016-matrix-external-validation-anchors-stage-gate.ps1
.\scripts\iso52016\assert-iso52016-matrix-external-validation-anchors-release-ready.ps1
```

## Generated evidence

The optional merge-summary writer creates generated files under:

```text
artifacts/iso52016/external-validation-anchors/
```

Those files are ignored and must not be committed.

## Release contract phrases

This stage is validation anchors only, not full equivalence claim.

No StandardReference equivalence is claimed or implied.

## Source type contract

The source policy string `IndependentManualEngineeringFormula` identifies independent manual engineering formulas as the reference source for these anchors.

Validation anchors only, not full equivalence claim.

## Source policy literal guard

- No StandardReference equivalence claim.

## Explicit non-claims

- No EnergyPlus comparison workflow claim.
No ASHRAE 140 / BESTEST-style validation anchor coverage claim.

No full ISO 52016 equivalence claim.

