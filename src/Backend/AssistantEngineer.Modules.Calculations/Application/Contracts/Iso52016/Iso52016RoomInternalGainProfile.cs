namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomInternalGainProfile(
    string RoomCode,
    int PeopleCount,
    double SensibleHeatGainPerPersonW,
    double EquipmentLoadW,
    double LightingLoadW,
    IReadOnlyList<Iso52016HourlyRoomInternalGainRecord> Hours)
{
    public int HourCount => Hours.Count;

    public double AnnualInternalGainsKWh =>
        Hours.Sum(hour => hour.TotalInternalGainW) / 1000.0;

    public Iso52016HourlyRoomInternalGainRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the room internal gain profile range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Room internal gain profile is not indexed by hour of year.");
        }

        return hour;
    }
}