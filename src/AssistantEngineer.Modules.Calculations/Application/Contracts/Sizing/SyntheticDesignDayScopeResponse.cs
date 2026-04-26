namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class SyntheticDesignDayScopeResponse
{
    public int? ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? ParentScopeName { get; set; }

    public double RawPeakLoadW { get; set; }
    public double RawPeakLoadKw { get; set; }

    public double SizedPeakLoadW { get; set; }
    public double SizedPeakLoadKw { get; set; }

    public double SafetyFactor { get; set; }

    public int PeakHourOfDay { get; set; }

    public List<SyntheticDesignDayHourResponse> Hours { get; set; } = new();
}