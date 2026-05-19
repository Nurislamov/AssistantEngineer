using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Scanning;

public sealed class NoDataOwnershipBackfillDryRunScanner : IOwnershipBackfillDryRunScanner
{
    public Task<OwnershipBackfillDryRunResult> ScanAsync(
        OwnershipBackfillOptions options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = DateTimeOffset.UtcNow;
        var runId = $"{startedAt:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..23];
        var completedAt = startedAt;

        var metrics = OwnershipBackfillConstants.KnownRecordTypes
            .Select(recordType => new OwnershipBackfillRecordTypeMetrics
            {
                RecordType = recordType,
                TotalRecords = 0,
                ResolvableRecords = 0,
                UnresolvedRecords = 0,
                AmbiguousRecords = 0,
                ResolvableRate = 0d,
                UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal)
            })
            .ToArray();

        var summary = new OwnershipBackfillDryRunSummary
        {
            RunId = runId,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            Mode = OwnershipBackfillRunMode.DryRun.ToString(),
            TotalRecordsScanned = 0,
            TotalRecordsResolvable = 0,
            TotalRecordsUnresolved = 0,
            UnresolvedByReason = new Dictionary<string, int>(StringComparer.Ordinal),
            RecordTypeMetrics = metrics,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        return Task.FromResult(new OwnershipBackfillDryRunResult
        {
            Summary = summary,
            UnresolvedRecords = Array.Empty<OwnershipBackfillUnresolvedRecord>(),
            PreviousValues = Array.Empty<OwnershipBackfillPreviousValueSnapshot>()
        });
    }
}
