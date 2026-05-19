namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public sealed class OwnershipBackfillGateOptions
{
    public required string EvidenceDirectory { get; init; }
    public string? SummaryPath { get; init; }
    public required string OutputDirectory { get; init; }
    public double MaxTotalUnresolvedRate { get; init; } = 0.05d;
    public double MaxProjectUnresolvedRate { get; init; } = 0d;
    public double MaxScenarioUnresolvedRate { get; init; } = 0.05d;
    public double MaxJobUnresolvedRate { get; init; } = 0.10d;
    public int MaxAmbiguousRecords { get; init; } = 0;
    public bool FailOnMissingRecordTypeMetrics { get; init; } = true;
    public bool FailOnAmbiguousRecords { get; init; } = true;
    public bool FailOnSchemaMismatch { get; init; } = true;
}
