using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyGeneratorInputValidatorTests
{
    private readonly SystemEnergyGeneratorInputValidator _validator = new();

    [Fact]
    public void AcceptsValidBoilerGeneratorInput()
    {
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMissingCalculationId()
    {
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput() with { CalculationId = string.Empty };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-CALCULATION-ID-MISSING");
    }

    [Fact]
    public void RejectsMissingGeneratorSet()
    {
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput() with { GeneratorSet = null! };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-SET-MISSING");
    }

    [Fact]
    public void RejectsUnknownGeneratorKind()
    {
        var generator = SystemEnergyTestData.CreateGenerator(kind: SystemEnergyGeneratorKind.Unknown);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            generatorSet: SystemEnergyTestData.CreateGeneratorSet([generator]));

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-KIND-UNKNOWN");
    }

    [Fact]
    public void RejectsUnknownCarrier()
    {
        var generator = SystemEnergyTestData.CreateGenerator(carrier: SystemEnergyCarrier.Unknown);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            generatorSet: SystemEnergyTestData.CreateGeneratorSet([generator]));

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-CARRIER-UNKNOWN");
    }

    [Fact]
    public void RejectsInvalidEfficiency()
    {
        var generator = SystemEnergyTestData.CreateGenerator(efficiency: 0.0);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            generatorSet: SystemEnergyTestData.CreateGeneratorSet([generator]));

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-EFFICIENCY-INVALID");
    }

    [Fact]
    public void RejectsInvalidCop()
    {
        var generator = SystemEnergyTestData.CreateGenerator(
            kind: SystemEnergyGeneratorKind.HeatPump,
            mode: SystemEnergyGeneratorCalculationMode.FixedCop,
            carrier: SystemEnergyCarrier.Electricity,
            efficiency: null,
            cop: 0.0);
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            generatorSet: SystemEnergyTestData.CreateGeneratorSet([generator]));

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-COP-INVALID");
    }

    [Fact]
    public void RejectsInvalidHourlyLoadFractionProfile()
    {
        var generator = SystemEnergyTestData.CreateGenerator(
            mode: SystemEnergyGeneratorCalculationMode.CustomFactor,
            efficiency: 0.9) with
        {
            HourlyLoadFraction8760 = Enumerable.Repeat(0.5, 8759).ToArray()
        };
        var input = SystemEnergyTestData.CreateGeneratorCalculationInput(
            generatorSet: SystemEnergyTestData.CreateGeneratorSet([generator], SystemEnergyLoadSplitMode.CustomHourlyFraction));

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-GEN-HOURLY-FRACTION-PROFILE-INVALID");
    }
}
