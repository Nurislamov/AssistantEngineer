namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillPlanOptions
{
    public string? EvidenceDirectory { get; init; }
    public string? GateResultPath { get; init; }
    public string? OutputDirectory { get; init; }
    public string RulesetVersion { get; init; } = "P6-05";
    public int? MaxPlannedRecords { get; init; }
    public bool IncludeLegacyUnscoped { get; init; }
    public bool ForceOverwrite { get; init; }
}
