using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;

namespace AssistantEngineer.Tests.Calculations.Solar;

public class IsotropicSkySurfaceIrradianceCalculatorTests
{
    private readonly IsotropicSkySurfaceIrradianceCalculator _calculator = new();

    [Fact]
    public void Calculate_ForHorizontalSurface_ReturnsBeamProjectedByZenithPlusDiffuse()
    {
        var position = CreateSolarPosition(
            altitudeDegrees: 30.0,
            azimuthDegrees: 180.0);

        var result = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: position,
                Surface: SurfaceOrientation.Horizontal,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 100.0,
                GlobalHorizontalIrradianceWm2: 500.0,
                GroundReflectance: 0.2));

        Assert.Equal(
            400.0,
            result.BeamIrradianceWm2,
            precision: 6);

        Assert.Equal(
            100.0,
            result.DiffuseSkyIrradianceWm2,
            precision: 6);

        Assert.Equal(
            0.0,
            result.GroundReflectedIrradianceWm2,
            precision: 6);

        Assert.Equal(
            500.0,
            result.TotalIrradianceWm2,
            precision: 6);
    }

    [Fact]
    public void Calculate_ForSouthVerticalSurfaceAtSouthernSun_ReturnsDirectBeam()
    {
        var position = CreateSolarPosition(
            altitudeDegrees: 30.0,
            azimuthDegrees: 180.0);

        var south = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: position,
                Surface: SurfaceOrientation.SouthVertical,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 100.0,
                GlobalHorizontalIrradianceWm2: 500.0,
                GroundReflectance: 0.2));

        var north = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: position,
                Surface: SurfaceOrientation.NorthVertical,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 100.0,
                GlobalHorizontalIrradianceWm2: 500.0,
                GroundReflectance: 0.2));

        Assert.True(
            south.BeamIrradianceWm2 > 0);

        Assert.Equal(
            0.0,
            north.BeamIrradianceWm2,
            precision: 6);

        Assert.True(
            south.TotalIrradianceWm2 > north.TotalIrradianceWm2);
    }

    [Fact]
    public void Calculate_WhenSunBelowHorizon_ReturnsNoBeamIrradiance()
    {
        var position = CreateSolarPosition(
            altitudeDegrees: -5.0,
            azimuthDegrees: 180.0);

        var result = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: position,
                Surface: SurfaceOrientation.SouthVertical,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 0.0,
                GlobalHorizontalIrradianceWm2: 0.0,
                GroundReflectance: 0.2));

        Assert.Equal(
            0.0,
            result.BeamIrradianceWm2,
            precision: 6);

        Assert.Equal(
            0.0,
            result.TotalIrradianceWm2,
            precision: 6);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(181.0)]
    public void Calculate_RejectsInvalidTilt(
        double tiltDegrees)
    {
        var position = CreateSolarPosition(
            altitudeDegrees: 30.0,
            azimuthDegrees: 180.0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(
                new SurfaceIrradianceRequest(
                    SolarPosition: position,
                    Surface: new SurfaceOrientation(
                        TiltDegrees: tiltDegrees,
                        AzimuthDegrees: 180.0),
                    DirectNormalIrradianceWm2: 800.0,
                    DiffuseHorizontalIrradianceWm2: 100.0,
                    GlobalHorizontalIrradianceWm2: 500.0)));
    }

    private static SolarPositionResult CreateSolarPosition(
        double altitudeDegrees,
        double azimuthDegrees)
    {
        return new SolarPositionResult(
            DayOfYear: 1,
            SolarDeclinationDegrees: 0.0,
            EquationOfTimeMinutes: 0.0,
            HourAngleDegrees: 0.0,
            SolarAltitudeDegrees: altitudeDegrees,
            SolarAzimuthDegrees: azimuthDegrees,
            ZenithAngleDegrees: 90.0 - altitudeDegrees,
            RelativeAirMass: altitudeDegrees > 0
                ? 1.0
                : 0.0);
    }
}