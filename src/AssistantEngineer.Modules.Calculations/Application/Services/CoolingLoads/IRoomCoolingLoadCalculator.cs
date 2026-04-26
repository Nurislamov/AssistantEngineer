using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;

public interface IRoomCoolingLoadCalculator
{
    Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<RoomCalculationResult> CalculateAsync(
        Room room,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}