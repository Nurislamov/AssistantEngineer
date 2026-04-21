namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public class VerificationMetrics
{
    public double Rmse { get; set; }
    public double MaxAbsoluteError { get; set; }
    public double PeakLoadDifferencePercent { get; set; }
    public bool WithinTolerance { get; set; }
}