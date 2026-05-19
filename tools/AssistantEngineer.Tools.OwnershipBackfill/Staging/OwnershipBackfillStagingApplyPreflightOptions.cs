namespace AssistantEngineer.Tools.OwnershipBackfill.Staging;

public sealed class OwnershipBackfillStagingApplyPreflightOptions
{
    public string? EnvironmentName { get; init; }
    public string? ApplyInputHash { get; init; }
    public string? ReadinessResultPath { get; init; }
    public string? PlanPath { get; init; }
    public string? SignoffPath { get; init; }
    public string? BackupReference { get; init; }
    public string? RollbackReadinessReference { get; init; }
    public string? OperatorId { get; init; }
    public string? SchemaVersion { get; init; }
    public bool EnableStagingApply { get; init; }
    public bool ConfirmNoProductionConnection { get; init; }
}
