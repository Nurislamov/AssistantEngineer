using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;

public interface IBuildingHeatingLoadCalculator
{
    Task<BuildingHeatingLoadResult> CalculateAsync(
        Building building,
        HeatingLoadCalculationMethod method = HeatingLoadCalculationMethod.En12831,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}