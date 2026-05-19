namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplyOptions
{
    public string? EvidenceDirectory { get; init; }
    public string? GateResultPath { get; init; }
    public string? PlanPath { get; init; }
    public string? PlanSignoffPath { get; init; }
    public string? OutputDirectory { get; init; }
    public string? DatabaseProvider { get; init; }
    public string? ConnectionString { get; init; }
    public bool EnableApply { get; init; }
    public string? ConfirmationPhrase { get; init; }
    public int BatchSize { get; init; } = 500;
}
