using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;

public interface IAggregateLoadCalculator
{
    Task<FloorCalculationResult> CalculateFloorAsync(
        Floor floor,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<FloorCalculationResult> CalculateFloorAsync(
        Floor floor,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<BuildingCalculationResult> CalculateBuildingAsync(
        Building building,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<BuildingCalculationResult> CalculateBuildingAsync(
        Building building,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}