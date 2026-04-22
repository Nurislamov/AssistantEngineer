using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IDomesticHotWaterFacade
{
    Result<DomesticHotWaterDemandResult> CalculateDemand(DomesticHotWaterDemandRequest request);
}
