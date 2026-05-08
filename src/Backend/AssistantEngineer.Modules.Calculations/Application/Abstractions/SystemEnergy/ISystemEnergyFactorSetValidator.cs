using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyFactorSetValidator
{
    SystemEnergyFactorSetValidationResult Validate(SystemEnergyFactorSet factorSet);
}
