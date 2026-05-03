using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;

namespace AssistantEngineer.Tests.Calculations.Solar;

public class PerezAnisotropicSurfaceIrradianceCalculatorTests
{
    private readonly PerezAnisotropicSurfaceIrradianceCalculator _calculator = new();

    [Fact]
    public void Calculate_WhenSunBelowHorizon_ClampsAllSolarComponentsToZero()
    {
        var result = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: CreateSolarPosition(
                    dayOfYear: 172,
                    altitudeDegrees: -5.0,
                    azimuthDegrees: 180.0),
                Surface: SurfaceOrientation.SouthVertical,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 100.0,
                GlobalHorizontalIrradianceWm2: 500.0));

        Assert.Equal(0.0, result.BeamIrradianceWm2, precision: 6);
        Assert.Equal(0.0, result.DiffuseSkyIrradianceWm2, precision: 6);
        Assert.Equal(0.0, result.GroundReflectedIrradianceWm2, precision: 6);
        Assert.Equal(0.0, result.TotalIrradianceWm2, precision: 6);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.NightSolarClampedToZero");
    }

    [Fact]
    public void Calculate_ForHorizontalSurface_ReturnsDniProjectedPlusDhiWhenGroundIsZero()
    {
        var position = CreateSolarPosition(
            dayOfYear: 172,
            altitudeDegrees: 30.0,
            azimuthDegrees: 180.0);

        var result = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: position,
                Surface: SurfaceOrientation.Horizontal,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 100.0,
                GlobalHorizontalIrradianceWm2: 0.0,
                GroundReflectance: 0.0));

        Assert.Equal(500.0, result.TotalIrradianceWm2, precision: 6);
        Assert.Equal(0.0, result.GroundReflectedIrradianceWm2, precision: 6);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "SolarWeather.PerezAnisotropicModelUsed");
    }

    [Fact]
    public void Calculate_SouthVerticalReceivesBeamWhenSunIsSouth()
    {
        var position = CreateSolarPosition(
            dayOfYear: 172,
            altitudeDegrees: 30.0,
            azimuthDegrees: 180.0);

        var south = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: position,
                Surface: SurfaceOrientation.SouthVertical,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 100.0,
                GlobalHorizontalIrradianceWm2: 500.0));

        var north = _calculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: position,
                Surface: SurfaceOrientation.NorthVertical,
                DirectNormalIrradianceWm2: 800.0,
                DiffuseHorizontalIrradianceWm2: 100.0,
                GlobalHorizontalIrradianceWm2: 500.0));

        Assert.True(south.BeamIrradianceWm2 > 0);
        Assert.Equal(0.0, north.BeamIrradianceWm2, precision: 6);
        Assert.True(south.TotalIrradianceWm2 > north.TotalIrradianceWm2);
    }

    [Fact]
    public void Calculate_PerezDiffuseDiffersFromIsotropicForTiltedSurface()
    {
        var position = CreateSolarPosition(
            dayOfYear: 172,
            altitudeDegrees: 20.0,
            azimuthDegrees: 180.0);

        var request = new SurfaceIrradianceRequest(
            SolarPosition: position,
            Surface: SurfaceOrientation.SouthVertical,
            DirectNormalIrradianceWm2: 300.0,
            DiffuseHorizontalIrradianceWm2: 200.0,
            GlobalHorizontalIrradianceWm2: 0.0,
            GroundReflectance: 0.2);

        var perez = _calculator.Calculate(request);
        var isotropic = new IsotropicSkySurfaceIrradianceCalculator().Calculate(request);

        Assert.True(
            Math.Abs(perez.DiffuseSkyIrradianceWm2 - isotropic.DiffuseSkyIrradianceWm2) > 1.0);

        Assert.True(perez.TotalIrradianceWm2 > 0);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(181.0)]
    public void Calculate_RejectsInvalidTilt(
        double tiltDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(
                new SurfaceIrradianceRequest(
                    SolarPosition: CreateSolarPosition(
                        dayOfYear: 172,
                        altitudeDegrees: 30.0,
                        azimuthDegrees: 180.0),
                    Surface: new SurfaceOrientation(
                        TiltDegrees: tiltDegrees,
                        AzimuthDegrees: 180.0),
                    DirectNormalIrradianceWm2: 800.0,
                    DiffuseHorizontalIrradianceWm2: 100.0,
                    GlobalHorizontalIrradianceWm2: 500.0)));
    }

    private static SolarPositionResult CreateSolarPosition(
        int dayOfYear,
        double altitudeDegrees,
        double azimuthDegrees)
    {
        var zenithDegrees = 90.0 - altitudeDegrees;

        return new SolarPositionResult(
            DayOfYear: dayOfYear,
            SolarDeclinationDegrees: 0.0,
            EquationOfTimeMinutes: 0.0,
            HourAngleDegrees: 0.0,
            SolarAltitudeDegrees: altitudeDegrees,
            SolarAzimuthDegrees: azimuthDegrees,
            ZenithAngleDegrees: zenithDegrees,
            RelativeAirMass: CalculateRelativeAirMass(zenithDegrees));
    }

    private static double CalculateRelativeAirMass(
        double zenithDegrees)
    {
        if (zenithDegrees >= 90.0)
            return 0.0;

        var zenithRadians = zenithDegrees * Math.PI / 180.0;

        return 1.0 /
               (
                   Math.Cos(zenithRadians) +
                   0.50572 * Math.Pow(
                       96.07995 - zenithDegrees,
                       -1.6364)
               );
    }
}
