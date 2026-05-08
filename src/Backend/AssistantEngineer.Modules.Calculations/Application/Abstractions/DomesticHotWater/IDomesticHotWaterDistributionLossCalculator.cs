using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterDistributionLossCalculator
{
    DomesticHotWaterLossComponentResult Calculate(
        DomesticHotWaterUsefulDemandResult usefulDemand,
        DomesticHotWaterDistributionLossInput input,
        double defaultAmbientTemperatureCelsius,
        double defaultRecoverableFraction);
}
