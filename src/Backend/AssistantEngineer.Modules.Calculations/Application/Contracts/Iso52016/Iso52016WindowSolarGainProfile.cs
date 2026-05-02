using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016WindowSolarGainProfile(
    CardinalDirection Orientation,
    double WindowAreaM2,
    double SolarHeatGainCoefficient,
    double FrameFraction,
    double ShadingFactor,
    IReadOnlyList<Iso52016HourlyWindowSolarGainRecord> Hours)
{
    public int HourCount => Hours.Count;

    public double AnnualSolarGainsKWh =>
        Hours.Sum(hour => hour.SolarGainW) / 1000.0;

    public Iso52016HourlyWindowSolarGainRecord GetHour(
        int hourOfYear)
    {
        if (hourOfYear < 0 || hourOfYear >= Hours.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourOfYear),
                "Hour of year is outside the window solar gain profile range.");
        }

        var hour = Hours[hourOfYear];

        if (hour.HourOfYear != hourOfYear)
        {
            throw new InvalidOperationException(
                "Window solar gain profile is not indexed by hour of year.");
        }

        return hour;
    }
}