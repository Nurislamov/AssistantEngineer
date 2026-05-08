using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyModuleCalculator
{
    SystemEnergyModuleResult Calculate(
        SystemEnergyModuleInput module,
        IReadOnlyList<double> hourlyInputEnergyKWh8760);
}
