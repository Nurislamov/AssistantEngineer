using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterCirculationLossCalculator
{
    IReadOnlyList<DomesticHotWaterLossComponentResult> Calculate(
        DomesticHotWaterUsefulDemandResult usefulDemand,
        DomesticHotWaterCirculationLossInput input,
        double defaultAmbientTemperatureCelsius,
        double defaultRecoverableFraction);
}
