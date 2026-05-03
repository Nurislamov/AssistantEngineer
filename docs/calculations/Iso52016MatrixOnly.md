# ISO 52016 Matrix-only decision

The project is not public yet, so the old simplified RC heat-balance path has been removed as a selectable or registered path.

The only supported ISO 52016 simulation engine is:

```csharp
Iso52016SimulationEngine.Matrix
```

## Removed path

The previous simplified room heat-balance solver was removed from the primary code path and from dependency injection.

Removed implementation guards:

- `IIso52016RoomHeatBalanceSolver`
- `Iso52016RoomHeatBalanceSolver`
- `Iso52016RoomHeatBalanceRequest`
- old heat-balance solver registration
- old heat-balance solver tests

## Kept contracts

The existing result contracts are kept to avoid unnecessary API churn:

- `Iso52016RoomEnergySimulationResult`
- `Iso52016RoomHeatBalanceProfile`
- `Iso52016HourlyRoomHeatBalanceResult`

These names describe result shape, not the removed solver implementation.

## Public/API behavior

The API no longer exposes `Legacy` or `V2Matrix` as supported values.

Use:

```json
{
  "simulationEngine": "Matrix"
}
```

The field may later be removed entirely if no alternate engines are planned.