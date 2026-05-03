namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Coupling between a thermal node and a named boundary condition, such as outdoor, ground, adjacent zone or supply air.
/// </summary>
public sealed record Iso52016V2BoundaryConductance(
    string NodeId,
    string BoundaryId,
    double ConductanceWPerK);