namespace AssistantEngineer.Tools.OwnershipBackfill.Production;

public sealed class OwnershipBackfillProductionPromotionOptions
{
    public string? StagingAcceptancePath { get; init; }
    public string? ProductionDryRunSummaryPath { get; init; }
    public string? ProductionGateResultPath { get; init; }
    public string? ProductionPlanPath { get; init; }
    public string? ProductionSignoffPath { get; init; }
    public string? ProductionReadinessPath { get; init; }
    public string? ProductionPreviousValuesPath { get; init; }
    public string? ProductionChangeRequestId { get; init; }
    public string? OutputDirectory { get; init; }
    public string RulesetVersion { get; init; } = "P6-13";
    public int MaxStagingAcceptanceAgeHours { get; init; } = 72;
    public int MaxProductionSignoffAgeHours { get; init; } = 24;
    public bool RequireSeparateProductionEvidence { get; init; } = true;
    public bool RequireBackupReference { get; init; } = true;
    public bool RequireRollbackReadiness { get; init; } = true;
}
