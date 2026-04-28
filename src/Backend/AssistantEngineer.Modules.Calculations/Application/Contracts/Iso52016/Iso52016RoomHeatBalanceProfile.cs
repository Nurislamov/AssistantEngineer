namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomHeatBalanceProfile(
    string RoomCode,
    double HeatingSetpointC,
    double CoolingSetpointC,
    Iso52016RoomHeatBalanceOptions Options,
    IReadOnlyList<Iso52016HourlyRoomHeatBalanceResult> Hours,
    IReadOnlyList<Iso52016MonthlyRoomHeatBalanceSummary> MonthlySummaries)
{
    public int HourCount => Hours.Count;

    public double AnnualHeatingEnergyKWh =>
        Hours.Sum(hour => hour.HeatingEnergyKWh);

    public double AnnualCoolingEnergyKWh =>
        Hours.Sum(hour => hour.CoolingEnergyKWh);

    public double AnnualSolarGainsKWh =>
        Hours.Sum(hour => hour.SolarGainsW) / 1000.0;

    public double AnnualInternalGainsKWh =>
        Hours.Sum(hour => hour.InternalGainsW) / 1000.0;

    public double AnnualTotalGainsKWh =>
        Hours.Sum(hour => hour.TotalGainsW) / 1000.0;

    public double PeakHeatingLoadW =>
        Hours.Count == 0
            ? 0.0
            : Hours.Max(hour => hour.HeatingLoadW);

    public double PeakCoolingLoadW =>
        Hours.Count == 0
            ? 0.0
            : Hours.Max(hour => hour.CoolingLoadW);

    public Iso52016HourlyRoomHeatBalanceResult GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the room heat balance profile range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Room heat balance profile is not indexed by hour of year.");
        }

        return hour;
    }
}