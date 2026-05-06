using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationDiagnosticsTests
{
    private readonly Iso16798NaturalVentilationCalculator _calculator = new();

    [Fact]
    public void NegativeOpeningArea_IsClampedWithDiagnostics()
    {
        var input = new Iso16798NaturalVentilationInput(
            RoomVolumeM3: 80,
            IndoorTemperatureC: 24,
            OutdoorTemperatureC: 20,
            WindSpeedMPerS: 2,
            AirDensityKgPerM3: 1.2,
            AirSpecificHeatJPerKgK: 1005,
            DischargeCoefficient: 0.6,
            MaximumAirChangesPerHour: 10,
            OpeningHeightM: 2,
            UsefulHeightDifferenceM: 2,
            WindPressureCoefficient: 0.5,
            WindExposureFactor: 1,
            StackCoefficient: 1,
            WindCoefficient: 1,
            Openings: new[]
            {
                new Iso16798NaturalVentilationOpeningInput("neg-area", -1.0, 0.8, true),
                new Iso16798NaturalVentilationOpeningInput("valid-area", 1.0, 0.5, true)
            });

        var result = _calculator.Calculate(input);

        Assert.Equal(0.5, result.EffectiveOpeningAreaM2, precision: 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Iso16798Ventilation.OpeningAreaClamped");
    }

    [Fact]
    public void Diagnostics_ContainModeAndClampInformation()
    {
        var fixture = Iso16798NaturalVentilationFixtureLoader.LoadById("stack-plus-wind-ach-clamped");

        var result = _calculator.Calculate(fixture.Input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Iso16798Ventilation.CalculationMode");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Iso16798Ventilation.AchClamped");
    }
}
