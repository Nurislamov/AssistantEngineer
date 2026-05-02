using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class DomesticHotWaterFacade : IDomesticHotWaterFacade
{
    private readonly DomesticHotWaterDemandService _domesticHotWater;

    public DomesticHotWaterFacade(
        DomesticHotWaterDemandService domesticHotWater)
    {
        _domesticHotWater = domesticHotWater;
    }

    public Result<DomesticHotWaterDemandResult> CalculateDomesticHotWaterDemand(
        DomesticHotWaterDemandRequest request) =>
        _domesticHotWater.Calculate(request);
}