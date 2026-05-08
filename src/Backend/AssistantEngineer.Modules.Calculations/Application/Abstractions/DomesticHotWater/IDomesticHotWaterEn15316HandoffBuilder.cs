using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterEn15316HandoffBuilder
{
    DomesticHotWaterEn15316Handoff Build(DomesticHotWaterSystemLoadResult result);
}
