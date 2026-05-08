using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGenerationHandoff(
    string CalculationId,
    IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyList<double>> HourlySystemLoadBeforeGenerationByEndUseKWh8760,
    IReadOnlyDictionary<SystemEnergyEndUse, double> AnnualSystemLoadBeforeGenerationByEndUseKWh,
    IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyList<double>> HourlyRecoverableLossByEndUseKWh8760,
    IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyList<double>> HourlyNonRecoverableLossByEndUseKWh8760,
    IReadOnlyList<SystemEnergyAuxiliaryLoadInput> AuxiliaryLoads,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
