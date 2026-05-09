namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneAnnualSummary(
    IReadOnlyDictionary<string, double> AnnualHeatingEnergyByZoneKWh,
    IReadOnlyDictionary<string, double> AnnualCoolingEnergyByZoneKWh)
{
    public double AnnualHeatingEnergyTotalKWh =>
        AnnualHeatingEnergyByZoneKWh.Values.Sum();

    public double AnnualCoolingEnergyTotalKWh =>
        AnnualCoolingEnergyByZoneKWh.Values.Sum();
}
