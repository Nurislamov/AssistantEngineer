using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyEmissionCalculator
{
    IReadOnlyList<SystemEnergyEmissionResult> Calculate(
        SystemEnergyFinalEnergyResult finalEnergyResult,
        SystemEnergyFactorSet factorSet);
}
