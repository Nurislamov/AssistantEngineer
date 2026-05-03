namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016MatrixHourlySolverRequest(
    string ZoneCode,
    IReadOnlyList<Iso52016MatrixNodeDefinition> Nodes,
    IReadOnlyList<Iso52016MatrixConductanceLink> InternalConductances,
    IReadOnlyList<Iso52016MatrixBoundaryConductance> BoundaryConductances,
    IReadOnlyList<Iso52016MatrixHourlyInputRecord> Hours,
    Iso52016MatrixHourlySolverOptions? Options = null);