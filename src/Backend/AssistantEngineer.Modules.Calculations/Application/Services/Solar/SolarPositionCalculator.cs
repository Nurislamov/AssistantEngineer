using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Solar;

internal sealed class SolarPositionCalculator : ISolarPositionCalculator
{
    public SolarPositionResult Calculate(
        SolarPositionRequest request)
    {
        Validate(request);

        var timestamp = request.Timestamp;
        var dayOfYear = timestamp.DayOfYear;

        var decimalHour =
            timestamp.Hour +
            timestamp.Minute / 60.0 +
            timestamp.Second / 3600.0;

        var fractionalYear = CalculateFractionalYear(
            dayOfYear,
            decimalHour);

        var equationOfTimeMinutes = CalculateEquationOfTimeMinutes(
            fractionalYear);

        var solarDeclinationRadians = CalculateSolarDeclinationRadians(
            fractionalYear);

        var timeZoneOffsetHours = timestamp.Offset.TotalHours;

        var timeOffsetMinutes =
            equationOfTimeMinutes +
            4.0 * request.LongitudeDegrees -
            60.0 * timeZoneOffsetHours;

        var trueSolarTimeMinutes =
            decimalHour * 60.0 +
            timeOffsetMinutes;

        trueSolarTimeMinutes %= 1440.0;

        if (trueSolarTimeMinutes < 0)
            trueSolarTimeMinutes += 1440.0;

        var hourAngleDegrees =
            trueSolarTimeMinutes / 4.0 - 180.0;

        var hourAngleRadians = SolarMath.ToRadians(
            hourAngleDegrees);

        var latitudeRadians = SolarMath.ToRadians(
            request.LatitudeDegrees);

        var cosZenith =
            Math.Sin(latitudeRadians) * Math.Sin(solarDeclinationRadians) +
            Math.Cos(latitudeRadians) * Math.Cos(solarDeclinationRadians) * Math.Cos(hourAngleRadians);

        cosZenith = SolarMath.Clamp(
            cosZenith,
            -1.0,
            1.0);

        var zenithRadians = Math.Acos(cosZenith);
        var zenithDegrees = SolarMath.ToDegrees(zenithRadians);
        var altitudeDegrees = 90.0 - zenithDegrees;

        var azimuthDegrees = CalculateSolarAzimuthDegrees(
            hourAngleRadians,
            latitudeRadians,
            solarDeclinationRadians);

        var relativeAirMass = CalculateRelativeAirMass(
            zenithDegrees);

        return new SolarPositionResult(
            DayOfYear: dayOfYear,
            SolarDeclinationDegrees: SolarMath.ToDegrees(solarDeclinationRadians),
            EquationOfTimeMinutes: equationOfTimeMinutes,
            HourAngleDegrees: hourAngleDegrees,
            SolarAltitudeDegrees: altitudeDegrees,
            SolarAzimuthDegrees: azimuthDegrees,
            ZenithAngleDegrees: zenithDegrees,
            RelativeAirMass: relativeAirMass);
    }

    private static double CalculateFractionalYear(
        int dayOfYear,
        double decimalHour) =>
        2.0 * Math.PI / 365.0 *
        (dayOfYear - 1 + (decimalHour - 12.0) / 24.0);

    private static double CalculateEquationOfTimeMinutes(
        double fractionalYear) =>
        229.18 *
        (
            0.000075 +
            0.001868 * Math.Cos(fractionalYear) -
            0.032077 * Math.Sin(fractionalYear) -
            0.014615 * Math.Cos(2.0 * fractionalYear) -
            0.040849 * Math.Sin(2.0 * fractionalYear)
        );

    private static double CalculateSolarDeclinationRadians(
        double fractionalYear) =>
        0.006918 -
        0.399912 * Math.Cos(fractionalYear) +
        0.070257 * Math.Sin(fractionalYear) -
        0.006758 * Math.Cos(2.0 * fractionalYear) +
        0.000907 * Math.Sin(2.0 * fractionalYear) -
        0.002697 * Math.Cos(3.0 * fractionalYear) +
        0.00148 * Math.Sin(3.0 * fractionalYear);

    private static double CalculateSolarAzimuthDegrees(
        double hourAngleRadians,
        double latitudeRadians,
        double solarDeclinationRadians)
    {
        var azimuthRadians = Math.Atan2(
            Math.Sin(hourAngleRadians),
            Math.Cos(hourAngleRadians) * Math.Sin(latitudeRadians) -
            Math.Tan(solarDeclinationRadians) * Math.Cos(latitudeRadians));

        return SolarMath.NormalizeDegrees360(
            SolarMath.ToDegrees(azimuthRadians) + 180.0);
    }

    private static double CalculateRelativeAirMass(
        double zenithDegrees)
    {
        if (zenithDegrees >= 90.0)
            return 0.0;

        var zenithRadians = SolarMath.ToRadians(
            zenithDegrees);

        return 1.0 /
               (
                   Math.Cos(zenithRadians) +
                   0.50572 * Math.Pow(
                       96.07995 - zenithDegrees,
                       -1.6364)
               );
    }

    private static void Validate(
        SolarPositionRequest request)
    {
        if (request.LatitudeDegrees is < -90.0 or > 90.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Latitude must be between -90 and 90 degrees.");
        }

        if (request.LongitudeDegrees is < -180.0 or > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Longitude must be between -180 and 180 degrees.");
        }
    }
}