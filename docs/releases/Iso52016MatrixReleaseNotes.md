# ISO 52016 Matrix release notes

## Status

The ISO 52016 Matrix calculation path is release-ready as an internal engineering-core stage.

## What changed

- Removed the old simplified `Legacy` solver path.
- Removed the temporary `simulationEngine` selector.
- Removed the `V2Matrix` public/internal naming and normalized the implementation to `ISO52016 Matrix`.
- Made the Matrix solver the only ISO 52016 calculation path.
- Added low-level deterministic Matrix baseline fixtures.
- Added application/building-facade Matrix baseline fixtures.
- Added independent manual steady-state validation anchors.
- Added baseline summary exporter.
- Added all-in-one Matrix verification command.
- Added Matrix release-ready gate.
- Added CI workflow for the Matrix release-ready gate.

## Primary verification command

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1
```

## CI workflow

```text
.github/workflows/iso52016-matrix-release-ready.yml
```

## Non-claims

This release does not claim:

- No exact StandardReference numerical equivalence claim.
- No exact EnergyPlus numerical equivalence claim.
- No ASHRAE 140 / BESTEST-style validation anchor coverage claim.
- No full coupled multi-zone heat-balance equivalence claim.
- No latent, humidity or moisture balance calculation claim.

## Merge recommendation

Before merging, run:

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit
```