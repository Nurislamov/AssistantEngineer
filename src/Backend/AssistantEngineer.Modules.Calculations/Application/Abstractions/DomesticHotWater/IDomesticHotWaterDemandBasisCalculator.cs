using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterDemandBasisCalculator
{
    DomesticHotWaterDemandBasisResult CalculateDailyVolume(DomesticHotWaterDemandBasisInput input);
}
