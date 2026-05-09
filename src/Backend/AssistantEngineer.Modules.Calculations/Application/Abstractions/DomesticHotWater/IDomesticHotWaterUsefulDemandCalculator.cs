using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;

public interface IDomesticHotWaterUsefulDemandCalculator
{
    DomesticHotWaterUsefulDemandResult Calculate(DomesticHotWaterUsefulDemandInput input);

    DomesticHotWaterDrawOffProfileResult Calculate(DomesticHotWaterDemandDefinition definition);
}
