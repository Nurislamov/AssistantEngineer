using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyDistributionCalculator
{
    SystemEnergyStageCalculationResult Calculate(SystemEnergyStageCalculationRequest request);
}
