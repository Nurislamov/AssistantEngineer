using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;

public interface ISystemEnergyHandoffUsefulDemandProvider
{
    Task<Result<BuildingEnergyBalanceResult>> CalculateUsefulDemandAsync(
        int buildingId,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        CancellationToken cancellationToken);
}
