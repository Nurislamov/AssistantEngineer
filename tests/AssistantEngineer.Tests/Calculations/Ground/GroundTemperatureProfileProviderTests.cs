using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundTemperatureProfileProviderTests
{
    private readonly GroundTemperatureProfileProvider _provider =
        new(new AnnualProfileShapeValidator(), new GroundTemperatureProfileCalculator());

    [Fact]
    public void BuildsProfileFromAnnualMean()
    {
        var climate = new GroundClimateInput(
            MonthlyOutdoorTemperaturesCelsius: null,
            HourlyOutdoorTemperaturesCelsius: null,
            AnnualMeanOutdoorTemperatureCelsius: 12.0,
            GroundTemperatureAmplitudeCelsius: null,
            GroundTemperaturePhaseShiftDays: null,
            Source: "UnitTest",
            Diagnostics: []);

        var result = _provider.BuildProfile(climate);

        Assert.Equal(12, result.MonthlyGroundBoundaryTemperaturesCelsius.Count);
        Assert.Equal(8760, result.HourlyGroundBoundaryTemperaturesCelsius.Count);
        Assert.All(result.MonthlyGroundBoundaryTemperaturesCelsius, AssertFinite);
        Assert.All(result.HourlyGroundBoundaryTemperaturesCelsius, AssertFinite);
    }

    [Fact]
    public void BuildsProfileFromMonthlyOutdoorTemperatures()
    {
        var climate = new GroundClimateInput(
            MonthlyOutdoorTemperaturesCelsius: [0, 1, 4, 8, 12, 16, 18, 17, 13, 8, 3, 1],
            HourlyOutdoorTemperaturesCelsius: null,
            AnnualMeanOutdoorTemperatureCelsius: null,
            GroundTemperatureAmplitudeCelsius: 3.0,
            GroundTemperaturePhaseShiftDays: 40.0,
            Source: "UnitTest",
            Diagnostics: []);

        var result = _provider.BuildProfile(climate);

        Assert.Equal(12, result.MonthlyGroundBoundaryTemperaturesCelsius.Count);
        Assert.Equal(8760, result.HourlyGroundBoundaryTemperaturesCelsius.Count);
    }

    [Fact]
    public void BuildsProfileFromHourlyOutdoorTemperatures()
    {
        var hourly = Enumerable.Range(0, 8760)
            .Select(hour => 10.0 + 8.0 * Math.Sin(2.0 * Math.PI * hour / 8760.0))
            .ToArray();

        var climate = new GroundClimateInput(
            MonthlyOutdoorTemperaturesCelsius: null,
            HourlyOutdoorTemperaturesCelsius: hourly,
            AnnualMeanOutdoorTemperatureCelsius: null,
            GroundTemperatureAmplitudeCelsius: 2.0,
            GroundTemperaturePhaseShiftDays: 20.0,
            Source: "UnitTest",
            Diagnostics: []);

        var result = _provider.BuildProfile(climate);

        Assert.Equal(12, result.MonthlyGroundBoundaryTemperaturesCelsius.Count);
        Assert.Equal(8760, result.HourlyGroundBoundaryTemperaturesCelsius.Count);
    }

    [Fact]
    public void ReportsDefaultAmplitudeAndPhaseShift()
    {
        var climate = new GroundClimateInput(
            MonthlyOutdoorTemperaturesCelsius: [1, 2, 5, 9, 14, 18, 20, 19, 14, 9, 4, 1],
            HourlyOutdoorTemperaturesCelsius: null,
            AnnualMeanOutdoorTemperatureCelsius: null,
            GroundTemperatureAmplitudeCelsius: null,
            GroundTemperaturePhaseShiftDays: null,
            Source: "UnitTest",
            Diagnostics: []);

        var result = _provider.BuildProfile(climate);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-AMPLITUDE-DEFAULTED");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-PHASE-SHIFT-DEFAULTED");
    }

    private static void AssertFinite(double value) =>
        Assert.True(double.IsFinite(value));
}
