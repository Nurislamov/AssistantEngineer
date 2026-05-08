using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGeneratorInput(
    string GeneratorId,
    string? Name,
    SystemEnergyGeneratorKind GeneratorKind,
    SystemEnergyGeneratorCalculationMode CalculationMode,
    SystemEnergyGeneratorServiceMode ServiceMode,
    SystemEnergyCarrier FinalEnergyCarrier,
    IReadOnlyList<SystemEnergyEndUse> ServedEndUses,
    int Priority,
    double? LoadFraction,
    double? NominalCapacityKWhPerHour,
    double? Efficiency,
    double? Cop,
    double? Eer,
    double? SeasonalPerformanceFactor,
    double? AuxiliaryElectricityFraction,
    double? AuxiliaryElectricityKWhPerKWhOutput,
    IReadOnlyList<double>? HourlyLoadFraction8760,
    IReadOnlyList<double>? HourlyFinalEnergyProfileKWh8760,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
