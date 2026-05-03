namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016MatrixHourlySolverProfile(
    string ZoneCode,
    Iso52016MatrixHourlySolverOptions Options,
    IReadOnlyList<Iso52016MatrixHourlyResult> Hours,
    IReadOnlyList<Iso52016MatrixMonthlySummary> MonthlySummaries)
{
    public int HourCount => Hours.Count;

    public double AnnualHeatingEnergyKWh => Hours.Sum(hour => hour.HeatingEnergyKWh);

    public double AnnualCoolingEnergyKWh => Hours.Sum(hour => hour.CoolingEnergyKWh);

    public double AnnualTotalNodeHeatGainsKWh => Hours.Sum(hour => hour.TotalNodeHeatGainsKWh);

    public double PeakHeatingLoadW => Hours.Count == 0 ? 0.0 : Hours.Max(hour => hour.HeatingLoadW);

    public double PeakCoolingLoadW => Hours.Count == 0 ? 0.0 : Hours.Max(hour => hour.CoolingLoadW);
}