using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class DomesticHotWaterFacade : IDomesticHotWaterFacade
{
    private readonly DomesticHotWaterDemandService _dhw;

    public DomesticHotWaterFacade(DomesticHotWaterDemandService dhw)
    {
        _dhw = dhw;
    }

    public Result<DomesticHotWaterDemandResult> CalculateDemand(DomesticHotWaterDemandRequest request) =>
        _dhw.Calculate(request);
}
