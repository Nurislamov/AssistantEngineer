using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370GroundTemperatureProfileTests
{
    private readonly Iso13370GroundTemperatureProfileCalculator _calculator = new();

    [Fact]
    public void BuildGroundMonthlyProfile_Returns12MonthsAndExpectedMean()
    {
        var profile = _calculator.BuildGroundMonthlyProfile(
            annualMeanTemperatureC: 11.0,
            amplitudeC: 4.0,
            phaseShiftMonths: 2.0);

        Assert.Equal(12, profile.Count);
        Assert.InRange(profile.Average(), 10.99, 11.01);
    }

    [Fact]
    public void ResolveOutdoorMonthlyProfile_UsesConstantAnnualMeanWhenMissing()
    {
        var profile = _calculator.ResolveOutdoorMonthlyProfile(
            outdoorMonthlyMeanTemperaturesC: null,
            outdoorAnnualMeanTemperatureC: 9.5);

        Assert.Equal(12, profile.Count);
        Assert.All(profile, value => Assert.Equal(9.5, value, 6));
    }
}
