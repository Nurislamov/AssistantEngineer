namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomHourlyInputProfile(
    string RoomCode,
    double TransmissionHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK,
    double HeatingSetpointC,
    double CoolingSetpointC,
    IReadOnlyList<Iso52016RoomHourlyInputRecord> Hours)
{
    public int HourCount => Hours.Count;

    public double TotalHeatTransferCoefficientWPerK =>
        TransmissionHeatTransferCoefficientWPerK +
        VentilationHeatTransferCoefficientWPerK;

    public double AnnualSolarGainsKWh =>
        Hours.Sum(hour => hour.SolarGainsW) / 1000.0;

    public double AnnualInternalGainsKWh =>
        Hours.Sum(hour => hour.InternalGainsW) / 1000.0;

    public double AnnualTotalGainsKWh =>
        Hours.Sum(hour => hour.TotalGainsW) / 1000.0;

    public Iso52016RoomHourlyInputRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the room hourly input profile range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Room hourly input profile is not indexed by hour of year.");
        }

        return hour;
    }
}