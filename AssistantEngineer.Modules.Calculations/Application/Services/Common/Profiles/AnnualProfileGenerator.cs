using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;

public sealed class AnnualProfileGenerator : IAnnualProfileGenerator
{
    private const int HoursPerYear = 8760;

    public AnnualProfile Generate(AnnualProfileRequest request)
    {
        ValidateDailyProfile(request.WeekdayProfile, nameof(request.WeekdayProfile));
        ValidateDailyProfile(request.WeekendProfile, nameof(request.WeekendProfile));

        var values = new double[HoursPerYear];
        var start = new DateTime(request.Year, 1, 1);
        for (var hourOfYear = 0; hourOfYear < values.Length; hourOfYear++)
        {
            var timestamp = start.AddHours(hourOfYear);
            var dayProfile = request.HolidayDates.Contains(DateOnly.FromDateTime(timestamp)) ||
                timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                    ? request.WeekendProfile
                    : request.WeekdayProfile;
            values[hourOfYear] = dayProfile[timestamp.Hour];
        }

        return new AnnualProfile(request.Name, request.Year, values);
    }

    private static void ValidateDailyProfile(IReadOnlyList<double> profile, string parameterName)
    {
        if (profile.Count != 24)
            throw new ArgumentException("Daily profile must contain exactly 24 values.", parameterName);

        if (profile.Any(value => value is < 0 or > 1 || !double.IsFinite(value)))
            throw new ArgumentException("Daily profile values must be finite and between 0 and 1.", parameterName);
    }
}