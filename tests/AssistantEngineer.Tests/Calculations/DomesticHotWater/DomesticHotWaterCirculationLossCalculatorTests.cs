using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterCirculationLossCalculatorTests
{
    private readonly DomesticHotWaterCirculationLossCalculator _calculator = new();

    [Fact]
    public void ReturnsZeroWhenCirculationNotPresent()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateCirculationInput(present: false);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        var thermal = result.Single(component => component.ComponentKind == DomesticHotWaterLossComponentKind.Circulation);
        Assert.Equal(0.0, thermal.AnnualLossKWh, 6);
    }

    [Fact]
    public void CalculatesCirculationThermalLoss()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateCirculationInput(
            loopLength: 30.0,
            linearLoss: 0.5,
            supply: 55.0,
            ambient: 20.0,
            operatingHours: 24.0,
            pumpPowerW: 0.0);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        var thermal = result.Single(component => component.ComponentKind == DomesticHotWaterLossComponentKind.Circulation);
        Assert.Equal(4599.0, thermal.AnnualLossKWh, 3);
    }

    [Fact]
    public void CalculatesPumpAuxiliaryElectricity()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateCirculationInput(
            pumpPowerW: 50.0,
            operatingHours: 24.0);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        var auxiliary = result.Single(component => component.ComponentKind == DomesticHotWaterLossComponentKind.AuxiliaryElectricity);
        Assert.Equal(438.0, auxiliary.AnnualLossKWh, 3);
        Assert.Contains(auxiliary.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-CIRCULATION-PUMP-AUXILIARY-CALCULATED");
    }

    [Fact]
    public void UsesHourlyOperationProfile()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var operation = Enumerable.Repeat(0.5, 8760).ToArray();
        var input = DomesticHotWaterSystemLossTestData.CreateCirculationInput(
            operation: operation,
            operatingHours: null,
            pumpPowerW: 0.0);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        var thermal = result.Single(component => component.ComponentKind == DomesticHotWaterLossComponentKind.Circulation);
        Assert.Equal(2299.5, thermal.AnnualLossKWh, 3);
        Assert.Contains(thermal.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-CIRCULATION-HOURLY-OPERATION-USED");
    }
}
