namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;

public sealed record AnnualWeatherDataSet(
    int Year,
    TimeSpan TimeZoneOffset,
    IReadOnlyList<HourlyWeatherRecord> Hours)
{
    public const int NonLeapYearHourCount = 8760;
    public const int LeapYearHourCount = 8784;

    public int HourCount => Hours.Count;

    public bool IsCompleteNonLeapYear =>
        IsCompleteYear(NonLeapYearHourCount);

    public bool IsCompleteLeapYear =>
        IsCompleteYear(LeapYearHourCount);

    public bool IsCompleteYear() =>
        IsCompleteNonLeapYear || IsCompleteLeapYear;

    public HourlyWeatherRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the dataset range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Weather dataset is not indexed by hour of year.");
        }

        return hour;
    }

    private bool IsCompleteYear(
        int expectedHourCount)
    {
        if (Hours.Count != expectedHourCount)
            return false;

        for (var i = 0; i < expectedHourCount; i++)
        {
            if (Hours[i].HourOfYear != i)
                return false;
        }

        return true;
    }
}