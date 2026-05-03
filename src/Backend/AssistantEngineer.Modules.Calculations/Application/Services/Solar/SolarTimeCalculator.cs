using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Solar;

internal sealed class SolarTimeCalculator : ISolarTimeCalculator
{
    public SolarTimeResult Calculate(
        DateTimeOffset timestamp,
        SolarLocation location)
    {
        Validate(location);

        var dayOfYear = timestamp.DayOfYear;

        var decimalLocalHour =
            timestamp.Hour +
            timestamp.Minute / 60.0 +
            timestamp.Second / 3600.0 +
            timestamp.Millisecond / 3_600_000.0;

        var fractionalYearRadians =
            2.0 * Math.PI / 365.0 *
            (dayOfYear - 1 + (decimalLocalHour - 12.0) / 24.0);

        var equationOfTimeMinutes =
            229.18 *
            (
                0.000075 +
                0.001868 * Math.Cos(fractionalYearRadians) -
                0.032077 * Math.Sin(fractionalYearRadians) -
                0.014615 * Math.Cos(2.0 * fractionalYearRadians) -
                0.040849 * Math.Sin(2.0 * fractionalYearRadians)
            );

        var longitudeCorrectionMinutes =
            4.0 * location.LongitudeDegrees -
            60.0 * location.UtcOffset.TotalHours;

        var timeOffsetMinutes =
            equationOfTimeMinutes +
            longitudeCorrectionMinutes;

        var trueSolarTimeMinutes =
            decimalLocalHour * 60.0 +
            timeOffsetMinutes;

        trueSolarTimeMinutes %= 1440.0;

        if (trueSolarTimeMinutes < 0)
            trueSolarTimeMinutes += 1440.0;

        var localSolarTimeHours =
            trueSolarTimeMinutes / 60.0;

        var hourAngleDegrees =
            trueSolarTimeMinutes / 4.0 -
            180.0;

        return new SolarTimeResult(
            DayOfYear: dayOfYear,
            DecimalLocalHour: decimalLocalHour,
            FractionalYearRadians: fractionalYearRadians,
            EquationOfTimeMinutes: equationOfTimeMinutes,
            LongitudeCorrectionMinutes: longitudeCorrectionMinutes,
            TimeOffsetMinutes: timeOffsetMinutes,
            TrueSolarTimeMinutes: trueSolarTimeMinutes,
            LocalSolarTimeHours: localSolarTimeHours,
            HourAngleDegrees: hourAngleDegrees);
    }

    private static void Validate(
        SolarLocation location)
    {
        if (location.LatitudeDegrees is < -90.0 or > 90.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(location),
                "Latitude must be between -90 and 90 degrees.");
        }

        if (location.LongitudeDegrees is < -180.0 or > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(location),
                "Longitude must be between -180 and 180 degrees.");
        }

        if (location.UtcOffset.TotalHours is < -14.0 or > 14.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(location),
                "UTC offset must be between -14 and +14 hours.");
        }
    }
}
