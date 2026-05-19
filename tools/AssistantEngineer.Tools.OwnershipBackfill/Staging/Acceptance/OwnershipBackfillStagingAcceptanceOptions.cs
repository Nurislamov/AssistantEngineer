namespace AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

public sealed class OwnershipBackfillStagingAcceptanceOptions
{
    public string? ApplyResultPath { get; init; }
    public string? PostApplyDryRunSummaryPath { get; init; }
    public string? PostApplyGateResultPath { get; init; }
    public string? TenantIsolationMatrixResultReference { get; init; }
    public string? RegressionTestResultReference { get; init; }
    public string? RollbackEvidenceReference { get; init; }
    public string? ApplyInputHash { get; init; }
    public string? PlanHash { get; init; }
    public string? SignoffId { get; init; }
    public string? ReadinessId { get; init; }
    public string? StagingPreflightReference { get; init; }
    public string? OperatorId { get; init; }
    public string? StagingChangeId { get; init; }
    public string? OutputDirectory { get; init; }
    public string RulesetVersion { get; init; } = "P6-12";
    public double MaxPostApplyUnresolvedRate { get; init; } = 0.01d;
    public bool RequireZeroFailedRecords { get; init; } = true;
    public bool RequireRollbackEvidence { get; init; } = true;
    public bool RequireTenantIsolationPass { get; init; } = true;
    public bool RequireRegressionPass { get; init; } = true;
}
