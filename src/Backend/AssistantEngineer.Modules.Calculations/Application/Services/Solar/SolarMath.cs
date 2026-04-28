namespace AssistantEngineer.Modules.Calculations.Application.Services.Solar;

internal static class SolarMath
{
    public const double DegreesToRadians = Math.PI / 180.0;
    public const double RadiansToDegrees = 180.0 / Math.PI;

    public static double ToRadians(
        double degrees) =>
        degrees * DegreesToRadians;

    public static double ToDegrees(
        double radians) =>
        radians * RadiansToDegrees;

    public static double Clamp(
        double value,
        double min,
        double max) =>
        Math.Min(
            Math.Max(value, min),
            max);

    public static double NormalizeDegrees360(
        double degrees)
    {
        var normalized = degrees % 360.0;

        return normalized < 0
            ? normalized + 360.0
            : normalized;
    }

    public static double PositiveOrZero(
        double value) =>
        value < 0
            ? 0
            : value;
}