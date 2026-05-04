# ISO 52016 Matrix engineering edge cases release gate

This release gate closes the ISO 52016 Matrix engineering edge-case hardening stage.

## Scope

Engineering edge-case hardening only.

Validation anchors only, not full parity.

The stage covers solver robustness and engineering sign/aggregation behavior for the Matrix calculation path:

- two-node free-floating implicit thermal response;
- adjacent/unconditioned boundary behavior;
- steady controlled timestep energy scaling;
- internal gain sign convention guards;
- annual and monthly aggregation guards.

## Commands

```powershell
.\scripts\iso52016\verify-iso52016-matrix-engineering-edge-cases-stage-gate.ps1
.\scripts\iso52016\assert-iso52016-matrix-engineering-edge-cases-release-ready.ps1
```

## Generated artifacts

The merge-summary writer may create generated files under:

```text
artifacts/iso52016/engineering-edge-cases/
```

Generated artifacts are ignored and must not be committed.

## Explicit non-claims

Engineering edge-case hardening only.

Validation anchors only, not full parity.

No pyBuildingEnergy parity claim.

No EnergyPlus parity claim.

No ASHRAE 140 validation coverage claim.

No full ISO 52016 parity claim.

No ExternalParityCovered claim.

No FullParityCovered claim.