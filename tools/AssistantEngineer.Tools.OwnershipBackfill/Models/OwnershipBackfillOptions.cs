namespace AssistantEngineer.Tools.OwnershipBackfill.Models;

public sealed record OwnershipBackfillOptions(
    int BatchSize,
    double MaxUnresolvedRate,
    string EvidenceOutputDirectory,
    string? ConnectionString,
    string DatabaseProvider,
    bool IncludeLegacyUnscoped,
    bool NoDataDryRun);
