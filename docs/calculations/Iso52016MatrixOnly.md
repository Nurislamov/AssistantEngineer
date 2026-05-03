# ISO 52016 Matrix-only decision

The project is not public yet, so the old simplified RC heat-balance path and the temporary simulation-engine selector have been removed.

There is now one single Matrix calculation path:

```text
ISO 52016 Matrix
```

## Removed public/internal selector

The following public/internal selector was removed because it no longer represents a real choice:

- `ISO 52016 calculation path`
- `calculationPath` API field
- `Matrix`
- `Matrix`

## Removed legacy solver path

The previous simplified room heat-balance solver was removed from dependency injection and from the primary source tree.

Removed implementation guards:

- `IIso52016RoomHeatBalanceSolver`
- `Iso52016RoomHeatBalanceSolver`
- `Iso52016RoomHeatBalanceRequest`
- old heat-balance solver registration
- old heat-balance solver tests

## Kept result contracts

The existing result contracts are kept to avoid unnecessary API churn:

- `Iso52016RoomEnergySimulationResult`
- `Iso52016RoomHeatBalanceProfile`
- `Iso52016HourlyRoomHeatBalanceResult`

These names describe result shape, not the removed solver implementation.

## Public/API behavior

The API has no public simulation engine selector. Requests should omit any `calculationPath` field.

The ISO 52016 simulation endpoint always uses the Matrix path.