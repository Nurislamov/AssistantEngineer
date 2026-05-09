using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundTemperatureProfileCalculatorTests
{
    private readonly GroundTemperatureProfileCalculator _calculator = new();

    [Fact]
    public void ConstantAnnualMean_ProducesConstantProfile()
    {
        var result = _calculator.Calculate(new GroundTemperatureProfileRequest(
            TimeResolution: GroundProfileTimeResolution.Hourly,
            Mode: GroundTemperatureProfileMode.ConstantAnnualMean,
            OutdoorTemperatureProfileCelsius: null,
            AnnualMeanOutdoorTemperatureCelsius: 10.0,
            GroundAnnualMeanTemperatureCelsius: 12.0,
            GroundTemperatureAmplitudeCelsius: 0.0,
            GroundTemperaturePhaseShiftDays: null,
            NumberOfSteps: 8760,
            TimeStepHours: 1.0));

        Assert.Equal(8760, result.GroundTemperatureProfileCelsius!.Count);
        Assert.All(result.GroundTemperatureProfileCelsius!, value => Assert.Equal(12.0, value, 6));
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-PROFILE-CONSTANT-ANNUAL-MEAN");
    }

    [Fact]
    public void SeasonalSinusoidal_AmplitudeChangesRange()
    {
        var low = _calculator.Calculate(CreateSeasonalRequest(amplitude: 2.0, phaseShiftDays: 45.0));
        var high = _calculator.Calculate(CreateSeasonalRequest(amplitude: 6.0, phaseShiftDays: 45.0));

        var lowRange = low.GroundTemperatureProfileCelsius!.Max() - low.GroundTemperatureProfileCelsius!.Min();
        var highRange = high.GroundTemperatureProfileCelsius!.Max() - high.GroundTemperatureProfileCelsius!.Min();

        Assert.True(highRange > lowRange);
    }

    [Fact]
    public void SeasonalSinusoidal_PhaseShiftMovesColdestPeriod()
    {
        var early = _calculator.Calculate(CreateSeasonalRequest(amplitude: 4.0, phaseShiftDays: 30.0));
        var late = _calculator.Calculate(CreateSeasonalRequest(amplitude: 4.0, phaseShiftDays: 120.0));

        var earlyColdestHour = IndexOfMin(early.GroundTemperatureProfileCelsius!);
        var lateColdestHour = IndexOfMin(late.GroundTemperatureProfileCelsius!);

        Assert.True(lateColdestHour > earlyColdestHour);
    }

    [Fact]
    public void InvalidAmplitude_EmitsDiagnostics()
    {
        var result = _calculator.Calculate(new GroundTemperatureProfileRequest(
            TimeResolution: GroundProfileTimeResolution.Hourly,
            Mode: GroundTemperatureProfileMode.SeasonalSinusoidal,
            OutdoorTemperatureProfileCelsius: null,
            AnnualMeanOutdoorTemperatureCelsius: null,
            GroundAnnualMeanTemperatureCelsius: 11.0,
            GroundTemperatureAmplitudeCelsius: double.NaN,
            GroundTemperaturePhaseShiftDays: null,
            NumberOfSteps: 24,
            TimeStepHours: 1.0));

        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-AMPLITUDE-DEFAULTED");
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-PHASE-SHIFT-DEFAULTED");
    }

    [Fact]
    public void MonthlyMode_ProducesDeterministicMonthlyAndHourlyLanes()
    {
        var result = _calculator.Calculate(new GroundTemperatureProfileRequest(
            TimeResolution: GroundProfileTimeResolution.Monthly,
            Mode: GroundTemperatureProfileMode.SeasonalSinusoidal,
            OutdoorTemperatureProfileCelsius: Enumerable.Repeat(8.0, 12).ToArray(),
            AnnualMeanOutdoorTemperatureCelsius: 8.0,
            GroundAnnualMeanTemperatureCelsius: 11.0,
            GroundTemperatureAmplitudeCelsius: 3.0,
            GroundTemperaturePhaseShiftDays: 60.0,
            NumberOfSteps: 12,
            TimeStepHours: 730.0));

        Assert.Equal(12, result.MonthlyGroundBoundaryTemperaturesCelsius.Count);
        Assert.Equal(8760, result.HourlyGroundBoundaryTemperaturesCelsius.Count);
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-PROFILE-EXPANDED-MONTHLY-TO-HOURLY");
    }

    private static GroundTemperatureProfileRequest CreateSeasonalRequest(
        double amplitude,
        double phaseShiftDays) =>
        new(
            TimeResolution: GroundProfileTimeResolution.Hourly,
            Mode: GroundTemperatureProfileMode.SeasonalSinusoidal,
            OutdoorTemperatureProfileCelsius: null,
            AnnualMeanOutdoorTemperatureCelsius: 10.0,
            GroundAnnualMeanTemperatureCelsius: 12.0,
            GroundTemperatureAmplitudeCelsius: amplitude,
            GroundTemperaturePhaseShiftDays: phaseShiftDays,
            NumberOfSteps: 8760,
            TimeStepHours: 1.0);

    private static int IndexOfMin(IReadOnlyList<double> values)
    {
        var best = 0;
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] < values[best])
                best = i;
        }

        return best;
    }
}
