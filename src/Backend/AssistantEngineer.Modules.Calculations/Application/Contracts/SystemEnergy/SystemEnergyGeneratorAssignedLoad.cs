using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGeneratorAssignedLoad(
    string GeneratorId,
    IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyList<double>> HourlyAssignedLoadByEndUseKWh8760,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
