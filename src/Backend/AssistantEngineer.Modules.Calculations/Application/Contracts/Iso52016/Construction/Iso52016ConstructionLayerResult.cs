namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

public sealed record Iso52016ConstructionLayerResult(
    string LayerId,
    string Name,
    double ThicknessM,
    double ResistanceM2KPerW,
    double ArealHeatCapacityJPerM2K,
    double EffectiveInternalCapacityContributionJPerM2K,
    bool IsMassless);
