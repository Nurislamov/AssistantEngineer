namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record AdjacentZoneBoundaryCondition(
    string ConditionId,
    IReadOnlyList<double> TemperatureProfileCelsius,
    bool IsAdiabaticEquivalent = false,
    string? Notes = null);
