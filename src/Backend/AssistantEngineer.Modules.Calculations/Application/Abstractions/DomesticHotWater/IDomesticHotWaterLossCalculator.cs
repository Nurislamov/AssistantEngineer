using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterLossCalculator
{
    DomesticHotWaterLossResult Calculate(
        IReadOnlyList<double> usefulDemandProfileKWh,
        DomesticHotWaterLossDefinition lossDefinition,
        IReadOnlyList<double>? hotWaterSetpointProfileCelsius);
}
