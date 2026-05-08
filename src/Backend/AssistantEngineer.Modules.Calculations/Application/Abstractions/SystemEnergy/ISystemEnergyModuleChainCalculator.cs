using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyModuleChainCalculator
{
    SystemEnergyModuleChainResult Calculate(SystemEnergyModuleChainInput input);
}
