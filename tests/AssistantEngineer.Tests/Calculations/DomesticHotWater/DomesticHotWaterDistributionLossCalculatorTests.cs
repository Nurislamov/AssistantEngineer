using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterDistributionLossCalculatorTests
{
    private readonly DomesticHotWaterDistributionLossCalculator _calculator = new();

    [Fact]
    public void ReturnsZeroWhenDistributionNotPresent()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateDistributionInput(present: false);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        Assert.Equal(0.0, result.AnnualLossKWh, 6);
    }

    [Fact]
    public void CalculatesDistributionLoss()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateDistributionInput(
            length: 20.0,
            linearLoss: 0.4,
            supply: 55.0,
            ambient: 20.0,
            operatingHours: 24.0);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        Assert.Equal(2452.8, result.AnnualLossKWh, 3);
    }

    [Fact]
    public void UsesUsefulDemandShapeWhenAvailable()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand(
            hourlyVolume: Enumerable.Range(0, 8760).Select(i => i % 24 == 12 ? 5.0 : 1.0).ToArray());
        var input = DomesticHotWaterSystemLossTestData.CreateDistributionInput();

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DISTRIBUTION-DEMAND-SHAPE-USED");
    }
}
