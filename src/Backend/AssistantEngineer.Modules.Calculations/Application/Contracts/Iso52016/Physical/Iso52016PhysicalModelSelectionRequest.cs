using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Application-facing adapter request for selecting the reduced Matrix model or the ISO52016-inspired physical node model.
/// This contract accepts an already prepared hourly input profile so it does not duplicate solar/weather/internal-gain pipelines.
/// </summary>
public sealed record Iso52016PhysicalModelSelectionRequest(
    Iso52016RoomHourlyInputProfile HourlyInputProfile,
    Iso52016PhysicalModelSelectionStrategy Strategy = Iso52016PhysicalModelSelectionStrategy.ReducedMatrix,
    Iso52016RoomHeatBalanceOptions? HeatBalanceOptions = null,
    Iso52016PhysicalNodeModelOptions? ModelOptions = null,
    IReadOnlyList<Iso52016PhysicalSurface>? Surfaces = null,
    IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition>? SurfaceBoundaryConditions = null,
    IReadOnlyList<Iso52016PhysicalHourlyOperationCondition>? OperationConditions = null);