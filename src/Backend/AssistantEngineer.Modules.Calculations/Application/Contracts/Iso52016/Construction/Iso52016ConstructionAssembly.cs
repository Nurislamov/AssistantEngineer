namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

public sealed record Iso52016ConstructionAssembly(
    string AssemblyId,
    string Name,
    Iso52016ConstructionBoundaryKind BoundaryKind,
    IReadOnlyList<Iso52016ConstructionMaterialLayer> Layers,
    double? InternalSurfaceResistanceM2KPerW = null,
    double? ExternalSurfaceResistanceM2KPerW = null,
    double? AreaM2 = null);
