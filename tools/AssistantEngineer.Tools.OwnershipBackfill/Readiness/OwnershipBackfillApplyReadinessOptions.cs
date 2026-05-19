namespace AssistantEngineer.Tools.OwnershipBackfill.Readiness;

public sealed class OwnershipBackfillApplyReadinessOptions
{
    public string? DryRunSummaryPath { get; init; }
    public string? GateResultPath { get; init; }
    public string? PlanPath { get; init; }
    public string? SignoffPath { get; init; }
    public string? PreviousValuesPath { get; init; }
    public string? OutputDirectory { get; init; }
    public string? ExpectedPlanHash { get; init; }
    public int MaxSignoffAgeHours { get; init; } = 24;
    public bool RequireRollbackReadiness { get; init; } = true;
    public string RulesetVersion { get; init; } = "P6-08";
}

