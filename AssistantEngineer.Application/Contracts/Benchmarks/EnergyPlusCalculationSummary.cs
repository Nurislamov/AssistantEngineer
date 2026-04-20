namespace AssistantEngineer.Application.Contracts.Benchmarks;

public class EnergyPlusCalculationSummary
{
    public double PeakCoolingLoadW { get; set; }
    public double PeakHeatingLoadW { get; set; }
    public List<double> HourlyCoolingLoadW { get; set; } = new();
    public List<double> HourlyHeatingLoadW { get; set; } = new();
}