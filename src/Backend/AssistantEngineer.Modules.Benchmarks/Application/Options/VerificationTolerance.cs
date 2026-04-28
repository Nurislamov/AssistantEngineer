namespace AssistantEngineer.Modules.Benchmarks.Application.Options;

internal sealed class VerificationTolerance
{
    public double RmseTolerance { get; set; } = 500;
    public double MaxAbsoluteErrorTolerance { get; set; } = 1000;
    public double PeakLoadTolerancePercent { get; set; } = 10;
}
