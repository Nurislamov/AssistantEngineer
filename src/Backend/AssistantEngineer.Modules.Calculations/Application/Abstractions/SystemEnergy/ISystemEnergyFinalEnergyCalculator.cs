using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyFinalEnergyCalculator
{
    SystemEnergyFinalEnergyResult Calculate(SystemEnergyGeneratorCalculationInput input);
}
