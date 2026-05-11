# P1 workflow input snapshot boundary status

P1 introduces `EngineeringWorkflowInputSnapshotBuilder` as the single workflow-facing boundary for collecting building/project inputs used by `EngineeringWorkflowStateBuilder`.

## Status

Closed for the P1 architecture stage.

## What changed

- `EngineeringWorkflowStateBuilder` no longer injects `IBuildingsFacade` directly.
- `EngineeringWorkflowStateBuilder` no longer injects `IEngineeringCoreStatusFacade` directly.
- Building/project/readiness/validation/core-status input collection is centralized behind `IEngineeringWorkflowInputSnapshotBuilder`.
- Current per-room wall/window/ventilation/ground queries are isolated in `EngineeringWorkflowInputSnapshotBuilder`.

## Current note after P3-03

P3-03 closes the N+1 follow-up for this boundary:

- `EngineeringWorkflowInputSnapshotBuilder` now uses a bulk workflow input query contract on `IBuildingsFacade`.
- per-room `GetRoomWallsAsync` / `GetRoomWindowsAsync` / `GetRoomVentilationParametersAsync` / `GetRoomGroundContactAsync` calls were removed from snapshot builder path.
- deterministic ordering remains enforced in bulk result mapping.

The boundary remains orchestration-only and does not add new engineering assumptions or calculation physics.
