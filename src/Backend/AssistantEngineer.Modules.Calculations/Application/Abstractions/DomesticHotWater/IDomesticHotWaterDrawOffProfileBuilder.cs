using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterDrawOffProfileBuilder
{
    DomesticHotWaterDrawOffProfileResult Build(DomesticHotWaterDrawOffProfileRequest request);
}
