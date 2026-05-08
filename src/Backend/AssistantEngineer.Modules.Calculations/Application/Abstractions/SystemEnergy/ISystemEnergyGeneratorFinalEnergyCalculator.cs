using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyGeneratorFinalEnergyCalculator
{
    SystemEnergyGeneratorResult Calculate(
        SystemEnergyGeneratorInput generator,
        SystemEnergyGeneratorAssignedLoad assignedLoad);
}
