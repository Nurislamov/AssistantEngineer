using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016WindowSolarGainCalculatorTests
{
    private readonly Iso52016WindowSolarGainCalculator _calculator = new();

    [Fact]
    public void Calculate_ReturnsSolarGainFromMappedSurfaceIrradiance()
    {
        var hour = CreateHour();

        var result = _calculator.Calculate(
            new Iso52016WindowSolarGainRequest(
                Hour: hour,
                Orientation: CardinalDirection.South,
                WindowAreaM2: 2.0,
                SolarHeatGainCoefficient: 0.6));

        Assert.True(result.IsSuccess);

        Assert.Equal(WeatherSolarSurfaceCodes.South, result.Value.SurfaceCode);
        Assert.Equal(2.0, result.Value.WindowAreaM2);
        Assert.Equal(2.0, result.Value.EffectiveGlazingAreaM2);
        Assert.Equal(600.0, result.Value.TotalSolarGainW, precision: 6);
    }

    [Fact]
    public void Calculate_AppliesFrameFraction()
    {
        var hour = CreateHour();

        var result = _calculator.Calculate(
            new Iso52016WindowSolarGainRequest(
                Hour: hour,
                Orientation: CardinalDirection.South,
                WindowAreaM2: 2.0,
                SolarHeatGainCoefficient: 0.6,
                FrameFraction: 0.25));

        Assert.True(result.IsSuccess);

        Assert.Equal(1.5, result.Value.EffectiveGlazingAreaM2, precision: 6);
        Assert.Equal(450.0, result.Value.TotalSolarGainW, precision: 6);
    }

    [Fact]
    public void Calculate_AppliesShadingFactor()
    {
        var hour = CreateHour();

        var result = _calculator.Calculate(
            new Iso52016WindowSolarGainRequest(
                Hour: hour,
                Orientation: CardinalDirection.South,
                WindowAreaM2: 2.0,
                SolarHeatGainCoefficient: 0.6,
                ShadingFactor: 0.5));

        Assert.True(result.IsSuccess);

        Assert.Equal(300.0, result.Value.TotalSolarGainW, precision: 6);
    }

    [Fact]
    public void Calculate_SplitsBeamDiffuseAndGroundGains()
    {
        var hour = CreateHour();

        var result = _calculator.Calculate(
            new Iso52016WindowSolarGainRequest(
                Hour: hour,
                Orientation: CardinalDirection.South,
                WindowAreaM2: 2.0,
                SolarHeatGainCoefficient: 0.6));

        Assert.True(result.IsSuccess);

        Assert.Equal(360.0, result.Value.BeamSolarGainW, precision: 6);
        Assert.Equal(180.0, result.Value.DiffuseSkySolarGainW, precision: 6);
        Assert.Equal(60.0, result.Value.GroundReflectedSolarGainW, precision: 6);
        Assert.Equal(600.0, result.Value.TotalSolarGainW, precision: 6);
    }

    [Theory]
    [InlineData(0.0, 0.6, 0.0, 1.0, "Window area must be greater than zero.")]
    [InlineData(2.0, -0.1, 0.0, 1.0, "Solar heat gain coefficient must be between 0 and 1.")]
    [InlineData(2.0, 1.1, 0.0, 1.0, "Solar heat gain coefficient must be between 0 and 1.")]
    [InlineData(2.0, 0.6, -0.1, 1.0, "Frame fraction must be greater than or equal to 0 and less than 1.")]
    [InlineData(2.0, 0.6, 1.0, 1.0, "Frame fraction must be greater than or equal to 0 and less than 1.")]
    [InlineData(2.0, 0.6, 0.0, -0.1, "Shading factor must be between 0 and 1.")]
    [InlineData(2.0, 0.6, 0.0, 1.1, "Shading factor must be between 0 and 1.")]
    public void Calculate_RejectsInvalidInputs(
        double area,
        double shgc,
        double frameFraction,
        double shadingFactor,
        string expectedError)
    {
        var result = _calculator.Calculate(
            new Iso52016WindowSolarGainRequest(
                Hour: CreateHour(),
                Orientation: CardinalDirection.South,
                WindowAreaM2: area,
                SolarHeatGainCoefficient: shgc,
                FrameFraction: frameFraction,
                ShadingFactor: shadingFactor));

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
    }

    private static Iso52016HourlyWeatherSolarRecord CreateHour()
    {
        var southSurface = new Iso52016SurfaceWeatherSolarRecord(
            SurfaceCode: WeatherSolarSurfaceCodes.South,
            Orientation: WeatherSolarSurface.South.Orientation,
            IncidenceAngleDegrees: 45,
            BeamIrradianceWm2: 300,
            DiffuseSkyIrradianceWm2: 150,
            GroundReflectedIrradianceWm2: 50,
            TotalIrradianceWm2: 500);

        return new Iso52016HourlyWeatherSolarRecord(
            HourOfYear: 12,
            Month: 1,
            Day: 1,
            Hour: 12,
            OutdoorTemperatureC: 10,
            GroundBoundaryTemperatureC: 12,
            SolarAltitudeDegrees: 30,
            SolarAzimuthDegrees: 180,
            DirectNormalIrradianceWm2: 600,
            DiffuseHorizontalIrradianceWm2: 100,
            GlobalHorizontalIrradianceWm2: 400,
            SurfaceIrradiance:
            [
                southSurface
            ]);
    }
}