namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

public sealed record Iso52016ConstructionAssemblyResult(
    double TotalResistanceM2KPerW,
    double UValueWPerM2K,
    double ArealHeatCapacityJPerM2K,
    double EffectiveInternalHeatCapacityJPerM2K,
    Iso52016ConstructionMassClass MassClass,
    IReadOnlyList<Iso52016ConstructionLayerResult> Layers,
    IReadOnlyList<Iso52016ConstructionNode> Nodes,
    Iso52016ConstructionNodeDistribution NodeDistribution,
    IReadOnlyList<Iso52016ConstructionDiagnostics> Diagnostics,
    IReadOnlyList<string> AssumptionsUsed);
