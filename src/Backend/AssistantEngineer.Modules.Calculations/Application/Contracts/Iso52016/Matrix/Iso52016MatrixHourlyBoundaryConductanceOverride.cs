namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

/// <summary>
/// Optional hourly override for a declared Matrix boundary conductance link.
/// This supports operation schedules such as hourly ventilation/infiltration without creating a new solver.
/// </summary>
public sealed record Iso52016MatrixHourlyBoundaryConductanceOverride(
    string NodeId,
    string BoundaryId,
    double ConductanceWPerK);
