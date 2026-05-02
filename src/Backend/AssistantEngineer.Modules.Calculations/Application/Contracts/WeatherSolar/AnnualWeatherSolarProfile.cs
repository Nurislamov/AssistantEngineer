namespace AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

public sealed record AnnualWeatherSolarProfile(
    int Year,
    TimeSpan TimeZoneOffset,
    double LatitudeDegrees,
    double LongitudeDegrees,
    IReadOnlyList<WeatherSolarSurface> Surfaces,
    IReadOnlyList<HourlyWeatherSolarRecord> Hours)
{
    public int HourCount => Hours.Count;

    public HourlyWeatherSolarRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the solar-weather profile range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Solar-weather profile is not indexed by hour of year.");
        }

        return hour;
    }
}