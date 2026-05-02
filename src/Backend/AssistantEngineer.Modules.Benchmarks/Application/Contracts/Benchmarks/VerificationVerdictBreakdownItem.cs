namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public class VerificationVerdictBreakdownItem
{
    public string Component { get; set; } = string.Empty;
    public bool HasComparableData { get; set; }
    public bool Passed { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
