using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyPrimaryEnergyCalculator
{
    SystemEnergyPrimaryEnergyResult Calculate(
        SystemEnergyFinalEnergyResult finalEnergyResult,
        SystemEnergyFactorSet factorSet);
}
