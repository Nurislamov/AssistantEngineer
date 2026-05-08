using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterStorageLossCalculator
{
    DomesticHotWaterLossComponentResult Calculate(
        DomesticHotWaterUsefulDemandResult usefulDemand,
        DomesticHotWaterStorageLossInput input,
        double defaultAmbientTemperatureCelsius,
        double defaultRecoverableFraction);
}
