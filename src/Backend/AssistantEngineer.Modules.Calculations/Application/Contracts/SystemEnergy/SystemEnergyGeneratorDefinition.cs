using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGeneratorDefinition(
    string GeneratorId,
    SystemEnergyUseKind UseKind,
    SystemEnergyGeneratorKind GeneratorKind,
    SystemEnergyCarrierKind CarrierKind,
    double? Efficiency,
    double? Cop,
    double? SeasonalPerformanceFactor,
    double? RenewableContributionFraction,
    IReadOnlyList<double>? AuxiliaryEnergyProfile,
    int Priority = 0,
    string? Source = null,
    IReadOnlyList<StandardCalculationDiagnostic>? Diagnostics = null);
