namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlySolverRequest(
    string ZoneCode,
    IReadOnlyList<Iso52016V2NodeDefinition> Nodes,
    IReadOnlyList<Iso52016V2ConductanceLink> InternalConductances,
    IReadOnlyList<Iso52016V2BoundaryConductance> BoundaryConductances,
    IReadOnlyList<Iso52016V2HourlyInputRecord> Hours,
    Iso52016V2HourlySolverOptions? Options = null);