namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

public sealed record Iso52016ConstructionNodeDistribution(
    IReadOnlyList<Iso52016ConstructionNode> Nodes,
    double TotalCapacityJPerM2K,
    double TotalResistanceM2KPerW);
