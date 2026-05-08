using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterStorageLossCalculatorTests
{
    private readonly DomesticHotWaterStorageLossCalculator _calculator = new();

    [Fact]
    public void ReturnsZeroWhenStorageNotPresent()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateStorageInput(present: false);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        Assert.Equal(0.0, result.AnnualLossKWh, 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-STORAGE-NOT-PRESENT");
    }

    [Fact]
    public void CalculatesStandingLoss()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateStorageInput(standingLossW: 100.0);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        Assert.Equal(876.0, result.AnnualLossKWh, 3);
    }

    [Fact]
    public void CalculatesCoefficientStorageLoss()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateStorageInput(
            standingLossW: null,
            coefficient: 2.0,
            setpoint: 60.0,
            ambient: 20.0);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        Assert.Equal(700.8, result.AnnualLossKWh, 3);
    }

    [Fact]
    public void SplitsRecoverableAndNonRecoverableLoss()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var input = DomesticHotWaterSystemLossTestData.CreateStorageInput(
            standingLossW: 100.0,
            recoverableFraction: 0.25);

        var result = _calculator.Calculate(useful, input, 20.0, 0.0);

        Assert.Equal(result.AnnualLossKWh * 0.25, result.AnnualRecoverableLossKWh, 3);
        Assert.Equal(result.AnnualLossKWh * 0.75, result.AnnualNonRecoverableLossKWh, 3);
    }
}
