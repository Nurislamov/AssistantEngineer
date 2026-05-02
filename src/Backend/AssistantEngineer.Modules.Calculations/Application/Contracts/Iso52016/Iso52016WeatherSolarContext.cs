using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016WeatherSolarContext(
    int Year,
    TimeSpan TimeZoneOffset,
    double LatitudeDegrees,
    double LongitudeDegrees,
    IReadOnlyList<Iso52016HourlyWeatherSolarRecord> Hours)
{
    public IReadOnlyList<CalculationDiagnostic> Diagnostics { get; init; } = [];

    public int HourCount => Hours.Count;

    public Iso52016HourlyWeatherSolarRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the ISO 52016 weather-solar context range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "ISO 52016 weather-solar context is not indexed by hour of year.");
        }

        return hour;
    }
}
