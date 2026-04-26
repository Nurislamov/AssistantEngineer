using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions;

public interface IBuildingEnergyCalculator
{
    Task<BuildingEnergyBalanceResult> CalculateAsync(
        Building building,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}