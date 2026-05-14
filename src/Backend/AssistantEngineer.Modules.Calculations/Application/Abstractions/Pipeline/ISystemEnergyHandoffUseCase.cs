using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;

public interface ISystemEnergyHandoffUseCase
{
    Task<Result<SystemEnergyHandoffResult>> CalculateBuildingSystemEnergyFromUsefulDemandAsync(
        int buildingId,
        CoolingLoadCalculationMethod coolingMethod = CoolingLoadCalculationMethod.Iso52016,
        HeatingLoadCalculationMethod heatingMethod = HeatingLoadCalculationMethod.En12831,
        DomesticHotWaterEn15316Handoff? dhwHandoff = null,
        CancellationToken cancellationToken = default);
}
