using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Calculations.Iso52016;

public interface IWindowShadingService
{
    double CalculateCombinedSolarReduction(
        CardinalDirection orientation,
        double latitudeDegrees,
        int dayOfYear,
        int hourOfDay,
        WindowShadingOptions options);
}

public sealed class WindowShadingService : IWindowShadingService
{
    public double CalculateCombinedSolarReduction(
        CardinalDirection orientation,
        double latitudeDegrees,
        int dayOfYear,
        int hourOfDay,
        WindowShadingOptions options)
    {
        if (options.OverhangDepthM <= 0 && options.SideFinDepthM <= 0 && options.RevealDepthM <= 0)
            return 1.0;

        var sun = CalculateSolarPosition(latitudeDegrees, dayOfYear, hourOfDay);
        if (sun.AltitudeDegrees <= 0)
            return 1.0;

        var surfaceAzimuth = ToSurfaceAzimuthDegrees(orientation);
        var relativeAzimuth = NormalizeDegrees(sun.AzimuthDegrees - surfaceAzimuth);
        if (Math.Abs(relativeAzimuth) >= 90)
            return 1.0;

        var windowHeight = Math.Max(0.1, options.WindowHeightM);
        var windowWidth = Math.Max(0.1, options.WindowWidthM);
        var relativeAzimuthRadians = DegreesToRadians(relativeAzimuth);
        var facadeIncidence = Math.Max(0, Math.Cos(relativeAzimuthRadians));
        var altitudeTan = Math.Tan(DegreesToRadians(sun.AltitudeDegrees));

        var overhangShadow = options.OverhangDepthM <= 0
            ? 0
            : options.OverhangDepthM * altitudeTan * facadeIncidence / windowHeight;
        var revealShadow = options.RevealDepthM <= 0
            ? 0
            : options.RevealDepthM * facadeIncidence / windowWidth;
        var sideFinShadow = options.SideFinDepthM <= 0
            ? 0
            : options.SideFinDepthM * Math.Abs(Math.Tan(relativeAzimuthRadians)) / windowWidth;

        var directBlockedFraction = Math.Clamp(overhangShadow + sideFinShadow + revealShadow, 0, 0.95);
        var directReduction = Math.Max(options.MinimumDirectSolarReductionFactor, 1 - directBlockedFraction);

        return Math.Clamp(
            options.DiffuseSolarShareUnaffected + (1 - options.DiffuseSolarShareUnaffected) * directReduction,
            options.MinimumDirectSolarReductionFactor,
            1.0);
    }

    private static (double AltitudeDegrees, double AzimuthDegrees) CalculateSolarPosition(
        double latitudeDegrees,
        int dayOfYear,
        int hourOfDay)
    {
        var latitude = DegreesToRadians(latitudeDegrees);
        var declination = DegreesToRadians(23.45 * Math.Sin(2 * Math.PI * (284 + dayOfYear) / 365.0));
        var solarHour = hourOfDay + 0.5;
        var hourAngle = DegreesToRadians(15 * (solarHour - 12.0));
        var sinAltitude = Math.Sin(latitude) * Math.Sin(declination) +
            Math.Cos(latitude) * Math.Cos(declination) * Math.Cos(hourAngle);
        var altitude = Math.Asin(Math.Max(0, sinAltitude)) * 180.0 / Math.PI;
        if (altitude <= 0)
            return (0, 0);

        var azimuthArgument = (Math.Sin(declination) - Math.Sin(latitude) * sinAltitude) /
            Math.Max(Math.Cos(latitude) * Math.Cos(Math.Asin(Math.Clamp(sinAltitude, -1, 1))), 0.0001);
        var azimuth = Math.Acos(Math.Clamp(azimuthArgument, -1, 1)) * 180.0 / Math.PI;
        if (solarHour > 12)
            azimuth = 360 - azimuth;

        return (altitude, azimuth);
    }

    private static double ToSurfaceAzimuthDegrees(CardinalDirection orientation) =>
        orientation switch
        {
            CardinalDirection.North => 0,
            CardinalDirection.NorthEast => 45,
            CardinalDirection.East => 90,
            CardinalDirection.SouthEast => 135,
            CardinalDirection.South => 180,
            CardinalDirection.SouthWest => 225,
            CardinalDirection.West => 270,
            CardinalDirection.NorthWest => 315,
            _ => 0
        };

    private static double NormalizeDegrees(double angle)
    {
        while (angle > 180)
            angle -= 360;
        while (angle < -180)
            angle += 360;
        return angle;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

public sealed record WindowShadingOptions(
    double OverhangDepthM,
    double SideFinDepthM,
    double RevealDepthM,
    double WindowHeightM,
    double WindowWidthM,
    double MinimumDirectSolarReductionFactor,
    double DiffuseSolarShareUnaffected);
