using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationPressureCalculatorTests
{
    private readonly NaturalVentilationPressureCalculator _calculator = new();

    [Fact]
    public void CalculatesWindPressure()
    {
        var opening = CreateOpening(
            windCp: 0.5,
            oppositeCp: 0.0,
            topHeight: 2.0,
            bottomHeight: 0.0,
            openingHeight: 2.0);
        var environment = CreateEnvironment(
            indoorTemperatureC: 22.0,
            outdoorTemperatureC: 12.0,
            windSpeed: 3.0,
            density: 1.2);

        var result = _calculator.CalculateWindPressure(opening, environment);

        Assert.NotNull(result.PressureDifferencePa);
        Assert.Equal(2.7, result.PressureDifferencePa!.Value, 6);
    }

    [Fact]
    public void CalculatesStackPressure()
    {
        var opening = CreateOpening(
            windCp: 0.0,
            oppositeCp: 0.0,
            topHeight: 2.0,
            bottomHeight: 0.0,
            openingHeight: 2.0);
        var environment = CreateEnvironment(
            indoorTemperatureC: 22.0,
            outdoorTemperatureC: 12.0,
            windSpeed: 0.0,
            density: 1.2);

        var result = _calculator.CalculateStackPressure(opening, environment);

        Assert.NotNull(result.PressureDifferencePa);
        Assert.True(result.PressureDifferencePa!.Value > 0.0);
    }

    [Fact]
    public void ZeroTemperatureDifferenceGivesZeroStackPressure()
    {
        var opening = CreateOpening(
            windCp: 0.0,
            oppositeCp: 0.0,
            topHeight: 2.0,
            bottomHeight: 0.0,
            openingHeight: 2.0);
        var environment = CreateEnvironment(
            indoorTemperatureC: 20.0,
            outdoorTemperatureC: 20.0,
            windSpeed: 0.0,
            density: 1.2);

        var result = _calculator.CalculateStackPressure(opening, environment);

        Assert.NotNull(result.PressureDifferencePa);
        Assert.Equal(0.0, result.PressureDifferencePa!.Value, 6);
    }

    [Fact]
    public void CombinedPressureUsesRootSumSquare()
    {
        var opening = CreateOpening(
            windCp: 1.0,
            oppositeCp: 0.0,
            topHeight: 1.0,
            bottomHeight: 0.0,
            openingHeight: 1.0);
        var environment = CreateEnvironment(
            indoorTemperatureC: 140.0,
            outdoorTemperatureC: 0.0,
            windSpeed: Math.Sqrt(6.0),
            density: 1.0);

        var wind = _calculator.CalculateWindPressure(opening, environment);
        var stack = _calculator.CalculateStackPressure(opening, environment);
        var combined = _calculator.CalculateCombinedPressure(opening, environment);

        Assert.NotNull(wind.PressureDifferencePa);
        Assert.NotNull(stack.PressureDifferencePa);
        Assert.NotNull(combined.PressureDifferencePa);
        Assert.Equal(3.0, wind.PressureDifferencePa!.Value, 6);
        Assert.Equal(4.0, stack.PressureDifferencePa!.Value, 1);
        Assert.Equal(5.0, combined.PressureDifferencePa!.Value, 1);
        Assert.Contains(combined.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-COMBINED-PRESSURE-RSS-USED");
    }

    private static NaturalVentilationOpeningGeometry CreateOpening(
        double windCp,
        double oppositeCp,
        double topHeight,
        double bottomHeight,
        double openingHeight) =>
        new(
            OpeningId: "O1",
            RoomId: "R1",
            ZoneId: "Z1",
            SurfaceId: "S1",
            OpeningType: NaturalVentilationOpeningType.Window,
            OpeningAreaSquareMeters: 1.0,
            OpeningHeightMeters: openingHeight,
            OpeningWidthMeters: 1.0,
            OpeningCenterHeightMeters: 1.5,
            BottomHeightMeters: bottomHeight,
            TopHeightMeters: topHeight,
            OpeningFraction: 1.0,
            DischargeCoefficient: 0.60,
            WindPressureCoefficient: windCp,
            OppositeWindPressureCoefficient: oppositeCp,
            OrientationAzimuthDegrees: 180.0,
            Source: "UnitTest",
            Diagnostics: []);

    private static NaturalVentilationEnvironment CreateEnvironment(
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeed,
        double density) =>
        new(
            IndoorTemperatureCelsius: indoorTemperatureC,
            OutdoorTemperatureCelsius: outdoorTemperatureC,
            WindSpeedMetersPerSecond: windSpeed,
            WindSpeedHeightMeters: 10.0,
            OpeningReferenceHeightMeters: 0.0,
            OutdoorAirDensityKgPerCubicMeter: density,
            IndoorAirDensityKgPerCubicMeter: density,
            AtmosphericPressurePa: 101325.0,
            Source: "UnitTest",
            Diagnostics: []);
}
