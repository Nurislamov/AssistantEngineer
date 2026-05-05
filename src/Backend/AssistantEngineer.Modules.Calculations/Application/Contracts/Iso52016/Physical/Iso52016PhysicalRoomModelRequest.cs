using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

public sealed record Iso52016PhysicalRoomModelRequest(
    Iso52016RoomHourlyInputProfile HourlyInputProfile,
    Iso52016RoomHeatBalanceOptions? HeatBalanceOptions = null,
    Iso52016PhysicalNodeModelOptions? ModelOptions = null,
    IReadOnlyList<Iso52016PhysicalSurface>? Surfaces = null,
    IReadOnlyList<Iso52016PhysicalSurfaceHourlyBoundaryCondition>? SurfaceBoundaryConditions = null,
    IReadOnlyList<Iso52016PhysicalHourlyOperationCondition>? OperationConditions = null);