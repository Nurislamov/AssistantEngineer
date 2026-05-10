# P1 workflow input snapshot boundary status

P1 introduces `EngineeringWorkflowInputSnapshotBuilder` as the single workflow-facing boundary for collecting building/project inputs used by `EngineeringWorkflowStateBuilder`.

## Status

Closed for the P1 architecture stage.

## What changed

- `EngineeringWorkflowStateBuilder` no longer injects `IBuildingsFacade` directly.
- `EngineeringWorkflowStateBuilder` no longer injects `IEngineeringCoreStatusFacade` directly.
- Building/project/readiness/validation/core-status input collection is centralized behind `IEngineeringWorkflowInputSnapshotBuilder`.
- Current per-room wall/window/ventilation/ground queries are isolated in `EngineeringWorkflowInputSnapshotBuilder`.

## Important limitation

This is a boundary extraction, not yet a repository-level batching implementation.

The current snapshot builder still performs per-room calls internally because the existing building facade does not yet expose bulk workflow-input methods. This is intentional for P1: it removes the N+4 loop from workflow state assembly and creates one narrow seam where repository-level batching can be added next.

## Next step

Replace the internal per-room loop in `EngineeringWorkflowInputSnapshotBuilder` with one bulk building workflow input query once the buildings module exposes an application contract such as:

```csharp
Task<Result<EngineeringWorkflowInputSnapshot>> GetWorkflowInputSnapshotAsync(
    int buildingId,
    int weatherYear,
    CancellationToken cancellationToken);
```

Until then, the architecture guard tests ensure the per-room calls do not leak back into `EngineeringWorkflowStateBuilder` or controllers.