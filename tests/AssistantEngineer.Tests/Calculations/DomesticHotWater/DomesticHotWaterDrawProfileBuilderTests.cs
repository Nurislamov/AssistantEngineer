using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterDrawProfileBuilderTests
{
    private readonly DomesticHotWaterDrawProfileBuilder _builder = new();

    [Fact]
    public void BuildsFlatProfileWhenNoProfileProvided()
    {
        var input = CreateInput();

        var result = _builder.Build(input);

        Assert.Equal(8760, result.AnnualHourlyFractions8760.Count);
        Assert.Equal(1.0, result.AnnualHourlyFractions8760.Sum(), 9);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DRAW-PROFILE-FLAT-DEFAULTED");
    }

    [Fact]
    public void BuildsAnnualProfileFrom24HourShape()
    {
        var input = CreateInput() with
        {
            HourlyFractions24 = Enumerable.Range(1, 24).Select(i => (double)i).ToArray()
        };

        var result = _builder.Build(input);

        Assert.Equal(8760, result.AnnualHourlyFractions8760.Count);
        Assert.Equal(1.0, result.AnnualHourlyFractions8760.Sum(), 9);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DRAW-PROFILE-BUILT-FROM-24H");
    }

    [Fact]
    public void AppliesMonthlyProfile()
    {
        var input = CreateInput() with
        {
            HourlyFractions24 = Enumerable.Repeat(1.0, 24).ToArray(),
            MonthlyFractions12 = new[] { 2.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 }
        };

        var result = _builder.Build(input);

        Assert.Equal(8760, result.AnnualHourlyFractions8760.Count);
        Assert.Equal(1.0, result.AnnualHourlyFractions8760.Sum(), 9);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DRAW-PROFILE-MONTHLY-APPLIED");
    }

    [Fact]
    public void UsesProvided8760Profile()
    {
        var input = CreateInput() with
        {
            AnnualHourlyFractions8760 = Enumerable.Repeat(1.0, 8760).ToArray()
        };

        var result = _builder.Build(input);

        Assert.Equal(8760, result.AnnualHourlyFractions8760.Count);
        Assert.Equal(1.0, result.AnnualHourlyFractions8760.Sum(), 9);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DRAW-PROFILE-BUILT-FROM-8760");
    }

    [Fact]
    public void InvalidProfileFallsBackToFlat()
    {
        var input = CreateInput() with
        {
            HourlyFractions24 = Enumerable.Repeat(0.0, 24).ToArray(),
            MonthlyFractions12 = null,
            AnnualHourlyFractions8760 = null
        };

        var result = _builder.Build(input);

        Assert.Equal(8760, result.AnnualHourlyFractions8760.Count);
        Assert.Equal(1.0, result.AnnualHourlyFractions8760.Sum(), 9);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-DRAW-PROFILE-INVALID-FALLBACK");
    }

    private static DomesticHotWaterDrawProfileInput CreateInput() =>
        new(
            ProfileId: "P1",
            HourlyFractions24: null,
            MonthlyFractions12: null,
            AnnualHourlyFractions8760: null,
            Source: "UnitTest",
            Diagnostics: []);
}
