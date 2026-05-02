namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomSolarGainProfile(
    string RoomCode,
    IReadOnlyList<Iso52016WindowSolarGainInput> Windows,
    IReadOnlyList<Iso52016HourlyRoomSolarGainRecord> Hours)
{
    public int HourCount => Hours.Count;

    public double AnnualSolarGainsKWh =>
        Hours.Sum(hour => hour.TotalSolarGainW) / 1000.0;

    public Iso52016HourlyRoomSolarGainRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the room solar gain profile range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Room solar gain profile is not indexed by hour of year.");
        }

        return hour;
    }
}