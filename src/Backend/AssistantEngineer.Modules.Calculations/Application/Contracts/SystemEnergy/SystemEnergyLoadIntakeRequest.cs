using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyLoadIntakeRequest(
    string CalculationId,
    IReadOnlyList<double>? HeatingUsefulProfileKWh,
    IReadOnlyList<double>? CoolingUsefulProfileKWh,
    DomesticHotWaterEn15316Handoff? DhwHandoff,
    IReadOnlyList<double>? AuxiliaryElectricityProfileKWh,
    double TimeStepHours,
    bool NormalizeSignedLoads = true,
    SystemEnergyLossOwnershipPolicy LossOwnershipPolicy = SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
    string? Source = null);
