using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationCalculatorTests
{
    private readonly Iso16798NaturalVentilationCalculator _calculator = new();

    [Fact]
    public void ClosedOpenings_ProduceZeroFlow()
    {
        var fixture = Iso16798NaturalVentilationFixtureLoader.LoadById("closed-openings-zero-flow");

        var result = _calculator.Calculate(fixture.Input);

        Assert.Equal(Iso16798NaturalVentilationCalculationMode.ClosedOpenings, result.CalculationMode);
        Assert.Equal(0.0, result.TotalAirflowM3PerS);
        Assert.Equal(0.0, result.ClampedAirChangesPerHour);
        Assert.Equal(0.0, result.HeatTransferCoefficientWPerK);
    }

    [Fact]
    public void StackOnlyCase_ProducesExpectedStackAirflow()
    {
        var fixture = Iso16798NaturalVentilationFixtureLoader.LoadById("stack-only-temperature-delta");

        var result = _calculator.Calculate(fixture.Input);

        Assert.Equal(Iso16798NaturalVentilationCalculationMode.StackOnly, result.CalculationMode);
        Assert.True(result.StackAirflowM3PerS > 0.0);
        Assert.Equal(0.0, result.WindAirflowM3PerS);
    }

    [Fact]
    public void WindOnlyCase_ProducesExpectedWindAirflow()
    {
        var fixture = Iso16798NaturalVentilationFixtureLoader.LoadById("wind-only-open-window");

        var result = _calculator.Calculate(fixture.Input);

        Assert.Equal(Iso16798NaturalVentilationCalculationMode.WindOnly, result.CalculationMode);
        Assert.Equal(0.0, result.StackAirflowM3PerS);
        Assert.True(result.WindAirflowM3PerS > 0.0);
    }

    [Fact]
    public void StackAndWindCase_ClampsAchWhenRequired()
    {
        var fixture = Iso16798NaturalVentilationFixtureLoader.LoadById("stack-plus-wind-ach-clamped");

        var result = _calculator.Calculate(fixture.Input);

        Assert.Equal(Iso16798NaturalVentilationCalculationMode.StackAndWind, result.CalculationMode);
        Assert.True(result.AirChangesPerHour > result.ClampedAirChangesPerHour);
        Assert.Equal(fixture.Input.MaximumAirChangesPerHour, result.ClampedAirChangesPerHour, precision: 6);
    }

    [Fact]
    public void HeatTransferCoefficient_IsConsistentWithClampedAirflow()
    {
        var fixture = Iso16798NaturalVentilationFixtureLoader.LoadById("stack-plus-wind-ach-clamped");

        var result = _calculator.Calculate(fixture.Input);
        var clampedFlowM3PerS = result.ClampedAirChangesPerHour * fixture.Input.RoomVolumeM3 / 3600.0;
        var expectedH = fixture.Input.AirDensityKgPerM3 * fixture.Input.AirSpecificHeatJPerKgK * clampedFlowM3PerS;

        Assert.Equal(expectedH, result.HeatTransferCoefficientWPerK, precision: 6);
    }
}
