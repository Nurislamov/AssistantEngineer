namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

public sealed record Iso52016ConstructionNode(
    string NodeId,
    string Name,
    double CapacityShareFraction,
    double CapacityJPerM2K,
    double ResistanceShareFraction);
