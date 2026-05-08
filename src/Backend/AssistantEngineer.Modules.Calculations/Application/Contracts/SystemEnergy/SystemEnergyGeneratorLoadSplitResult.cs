using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGeneratorLoadSplitResult(
    IReadOnlyList<SystemEnergyGeneratorAssignedLoad> AssignedLoads,
    IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyList<double>> HourlyUnassignedLoadByEndUseKWh8760,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
