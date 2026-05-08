using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyFactorSet(
    string FactorSetId,
    IReadOnlyList<SystemEnergyPrimaryEnergyFactor> PrimaryEnergyFactors,
    IReadOnlyList<SystemEnergyEmissionFactor> EmissionFactors,
    string? Region,
    int? Year,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
