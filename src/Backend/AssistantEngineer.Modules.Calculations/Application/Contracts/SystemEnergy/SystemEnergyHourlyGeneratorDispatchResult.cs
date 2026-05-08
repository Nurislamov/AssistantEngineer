using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyHourlyGeneratorDispatchResult(
    int HourIndex,
    string GeneratorId,
    SystemEnergyGeneratorKind GeneratorKind,
    SystemEnergyEndUse EndUse,
    double RequestedSystemLoadKWh,
    double SuppliedSystemLoadKWh,
    double UnmetSystemLoadKWh,
    double FinalEnergyKWh,
    SystemEnergyCarrier FinalEnergyCarrier,
    double AuxiliaryElectricityKWh,
    SystemEnergyFinalEnergyStatus Status,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
