namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016GroundBoundaryTemperatureProfile(
    int Year,
    IReadOnlyList<Iso52016GroundBoundaryTemperatureRecord> Hours)
{
    public int HourCount => Hours.Count;

    public Iso52016GroundBoundaryTemperatureRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the ground boundary temperature profile range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Ground boundary temperature profile is not indexed by hour of year.");
        }

        return hour;
    }
}