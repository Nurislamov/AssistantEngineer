namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016BuildingEnergySimulationApplicationResult(
    int BuildingId,
    string BuildingName,
    int ClimateZoneId,
    int WeatherYear,
    double LatitudeDegrees,
    double LongitudeDegrees,
    TimeSpan TimeZoneOffset,
    Iso52016BuildingDomainSimulationFacadeResult Simulation)
{
public int RoomCount => Simulation.RoomCount;

    public int HourCount => Simulation.HourCount;

    public double AnnualHeatingEnergyKWh =>
        Simulation.AnnualHeatingEnergyKWh;

    public double AnnualCoolingEnergyKWh =>
        Simulation.AnnualCoolingEnergyKWh;

    public double AnnualSolarGainsKWh =>
        Simulation.AnnualSolarGainsKWh;

    public double AnnualInternalGainsKWh =>
        Simulation.AnnualInternalGainsKWh;

    public double AnnualTotalGainsKWh =>
        Simulation.AnnualTotalGainsKWh;

    public double PeakHeatingLoadW =>
        Simulation.PeakHeatingLoadW;

    public double PeakCoolingLoadW =>
        Simulation.PeakCoolingLoadW;
}