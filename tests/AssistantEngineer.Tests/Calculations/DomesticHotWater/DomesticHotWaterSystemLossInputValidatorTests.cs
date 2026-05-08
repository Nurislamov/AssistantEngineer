using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterSystemLossInputValidatorTests
{
    private readonly DomesticHotWaterSystemLossInputValidator _validator = new();

    [Fact]
    public void AcceptsValidSystemLossInput()
    {
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMissingCalculationId()
    {
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput() with { CalculationId = "" };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-SYSTEM-CALCULATION-ID-MISSING");
    }

    [Fact]
    public void RejectsInvalidUsefulHourlyProfile()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand(
            hourlyUsefulEnergy: Enumerable.Repeat(1.0, 8759).ToArray());
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput(usefulDemand: useful);

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-SYSTEM-USEFUL-HOURLY-PROFILE-INVALID");
    }

    [Fact]
    public void RejectsInvalidDefaultRecoverableFraction()
    {
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput(defaultRecoverableFraction: 1.5);

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-SYSTEM-DEFAULT-RECOVERABLE-FRACTION-INVALID");
    }

    [Fact]
    public void ReportsIncompleteStorageInput()
    {
        var storage = DomesticHotWaterSystemLossTestData.CreateStorageInput(
            present: true,
            standingLossW: null,
            coefficient: null,
            setpoint: null,
            ambient: null);
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput(storage: storage);

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-STORAGE-INPUT-INCOMPLETE");
    }

    [Fact]
    public void ReportsInvalidDistributionInput()
    {
        var distribution = DomesticHotWaterSystemLossTestData.CreateDistributionInput(length: 0.0);
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput(distribution: distribution);

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DISTRIBUTION-PIPE-LENGTH-INVALID");
    }

    [Fact]
    public void ReportsInvalidCirculationOperationProfile()
    {
        var circulation = DomesticHotWaterSystemLossTestData.CreateCirculationInput(
            operation: Enumerable.Repeat(0.5, 8759).ToArray());
        var input = DomesticHotWaterSystemLossTestData.CreateSystemLossInput(circulation: circulation);

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-CIRCULATION-OPERATION-PROFILE-INVALID");
    }
}
