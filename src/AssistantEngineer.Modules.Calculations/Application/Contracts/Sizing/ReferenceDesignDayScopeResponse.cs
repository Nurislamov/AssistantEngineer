namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class ReferenceDesignDayScopeResponse
{
    public int? ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? ParentScopeName { get; set; }

    public int DayOfYear { get; set; }
    public int PeakHourOfYear { get; set; }
    public int PeakMonth { get; set; }

    public double RawPeakLoadW { get; set; }
    public double RawPeakLoadKw { get; set; }

    public double SizedPeakLoadW { get; set; }
    public double SizedPeakLoadKw { get; set; }

    public double SafetyFactor { get; set; }

    public List<ReferenceDesignDayHourResponse> Hours { get; set; } = new();
}