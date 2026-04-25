namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class PeakLoadSummaryResponse
{
    public int? ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? ParentScopeName { get; set; }

    public double RawPeakLoadW { get; set; }
    public double RawPeakLoadKw { get; set; }

    public double SizedPeakLoadW { get; set; }
    public double SizedPeakLoadKw { get; set; }

    public double SafetyFactor { get; set; }

    public int PeakHourOfYear { get; set; }
    public int Month { get; set; }

    public double OperativeTemperatureC { get; set; }
    public double OutdoorTemperatureC { get; set; }
}